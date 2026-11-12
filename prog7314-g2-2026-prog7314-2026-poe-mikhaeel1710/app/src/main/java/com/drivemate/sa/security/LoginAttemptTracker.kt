package com.drivemate.sa.security

import android.content.Context
import java.security.MessageDigest

/**
 * Where failed-attempt state is kept. Abstracted so the lockout logic can be
 * unit tested on the JVM without Android.
 */
interface AttemptStore {
    fun getFailedCount(key: String): Int
    fun getLockedUntil(key: String): Long
    fun save(key: String, failedCount: Int, lockedUntilMillis: Long)
    fun clear(key: String)
}

/** SharedPreferences-backed store. Excluded from backup (see backup rules). */
class SharedPrefsAttemptStore(context: Context) : AttemptStore {

    private val prefs = context.applicationContext.getSharedPreferences(
        PREFS_NAME,
        Context.MODE_PRIVATE
    )

    override fun getFailedCount(key: String): Int =
        prefs.getInt("count_$key", 0)

    override fun getLockedUntil(key: String): Long =
        prefs.getLong("until_$key", 0L)

    override fun save(key: String, failedCount: Int, lockedUntilMillis: Long) {
        prefs.edit()
            .putInt("count_$key", failedCount)
            .putLong("until_$key", lockedUntilMillis)
            .apply()
    }

    override fun clear(key: String) {
        prefs.edit()
            .remove("count_$key")
            .remove("until_$key")
            .apply()
    }

    companion object {
        const val PREFS_NAME = "drivemate_login_attempts"
    }
}

/**
 * Limits password sign-in attempts: after [MAX_ATTEMPTS] consecutive failed
 * attempts for an email address the account is locked out on this device for
 * [LOCKOUT_DURATION_MILLIS]. A successful sign-in resets the counter.
 *
 * Emails are hashed before being used as storage keys so addresses are not
 * written to disk in plain text.
 */
class LoginAttemptTracker(private val store: AttemptStore) {

    sealed class State {
        /** Sign-in is allowed. [attemptsRemaining] failed tries are left. */
        data class Allowed(val attemptsRemaining: Int) : State()

        /** Sign-in is blocked until [lockedUntilMillis]. */
        data class LockedOut(val lockedUntilMillis: Long) : State()
    }

    fun check(email: String, nowMillis: Long = System.currentTimeMillis()): State {
        val key = keyFor(email)
        val lockedUntil = store.getLockedUntil(key)

        if (lockedUntil > nowMillis) {
            return State.LockedOut(lockedUntil)
        }

        if (lockedUntil != 0L) {
            // Lockout has expired: start again with a fresh allowance.
            store.clear(key)
        }

        val failed = store.getFailedCount(key)
        return State.Allowed((MAX_ATTEMPTS - failed).coerceAtLeast(0))
    }

    /** Call after a wrong-password / invalid-credential result. */
    fun recordFailure(
        email: String,
        nowMillis: Long = System.currentTimeMillis()
    ): State {
        val key = keyFor(email)

        // Don't keep counting while already locked out.
        val current = check(email, nowMillis)
        if (current is State.LockedOut) return current

        val failed = store.getFailedCount(key) + 1

        return if (failed >= MAX_ATTEMPTS) {
            val until = nowMillis + LOCKOUT_DURATION_MILLIS
            store.save(key, failed, until)
            State.LockedOut(until)
        } else {
            store.save(key, failed, 0L)
            State.Allowed(MAX_ATTEMPTS - failed)
        }
    }

    /** Call after a successful sign-in. */
    fun recordSuccess(email: String) {
        store.clear(keyFor(email))
    }

    private fun keyFor(email: String): String {
        val normalised = email.trim().lowercase()
        val digest = MessageDigest.getInstance("SHA-256")
            .digest(normalised.toByteArray(Charsets.UTF_8))
        return digest.joinToString("") { "%02x".format(it) }
    }

    companion object {
        const val MAX_ATTEMPTS = 15
        const val LOCKOUT_DURATION_MILLIS = 15L * 60L * 1000L // 15 minutes
    }
}
