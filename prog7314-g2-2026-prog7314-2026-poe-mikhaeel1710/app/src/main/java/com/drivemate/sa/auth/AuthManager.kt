package com.drivemate.sa.auth

import android.content.Context
import com.drivemate.sa.security.LoginAttemptTracker
import com.drivemate.sa.security.SharedPrefsAttemptStore
import com.google.firebase.FirebaseNetworkException
import com.google.firebase.FirebaseTooManyRequestsException
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.auth.FirebaseAuthInvalidCredentialsException
import com.google.firebase.auth.FirebaseAuthInvalidUserException
import com.google.firebase.auth.FirebaseAuthUserCollisionException
import com.google.firebase.auth.FirebaseAuthWeakPasswordException
import kotlinx.coroutines.tasks.await
import kotlin.math.ceil

sealed class AuthResult {
    object Success : AuthResult()
    data class Locked(val minutes: Long) : AuthResult()
    data class BadCredentials(val attemptsLeft: Int) : AuthResult()
    data class Error(val message: String) : AuthResult()
}

class AuthManager(
    context: Context,
    private val tracker: LoginAttemptTracker =
        LoginAttemptTracker(SharedPrefsAttemptStore(context.applicationContext))
) {
    private val auth = FirebaseAuth.getInstance()

    suspend fun register(email: String, password: String): AuthResult {
        if (password.length < 8) return AuthResult.Error("Password must be at least 8 characters.")
        return try {
            val user = auth.createUserWithEmailAndPassword(email.trim(), password).await().user
            user?.sendEmailVerification()
            AuthResult.Success
        } catch (e: FirebaseAuthUserCollisionException) {
            AuthResult.Error("An account with this email already exists.")
        } catch (e: FirebaseAuthWeakPasswordException) {
            AuthResult.Error("That password is too weak.")
        } catch (e: Exception) {
            AuthResult.Error("Registration failed. Check your connection and try again.")
        }
    }

    suspend fun login(email: String, password: String): AuthResult {
        val cleanEmail = email.trim()

        val state = tracker.check(cleanEmail)
        if (state is LoginAttemptTracker.State.LockedOut) {
            return AuthResult.Locked(minutesUntil(state.lockedUntilMillis))
        }

        return try {
            auth.signInWithEmailAndPassword(cleanEmail, password).await()
            tracker.recordSuccess(cleanEmail)
            AuthResult.Success
        } catch (e: FirebaseAuthInvalidCredentialsException) {
            failureResult(cleanEmail)
        } catch (e: FirebaseAuthInvalidUserException) {
            failureResult(cleanEmail)
        } catch (e: FirebaseTooManyRequestsException) {
            AuthResult.Error("Too many attempts. Please try again later.")
        } catch (e: FirebaseNetworkException) {
            AuthResult.Error("Could not reach the sign-in service. Check your connection.")
        } catch (e: Exception) {
            AuthResult.Error("Sign-in failed. Check your connection.")
        }
    }

    private fun failureResult(email: String): AuthResult =
        when (val result = tracker.recordFailure(email)) {
            is LoginAttemptTracker.State.LockedOut ->
                AuthResult.Locked(minutesUntil(result.lockedUntilMillis))
            is LoginAttemptTracker.State.Allowed ->
                AuthResult.BadCredentials(result.attemptsRemaining)
        }

    private fun minutesUntil(lockedUntilMillis: Long): Long =
        ceil((lockedUntilMillis - System.currentTimeMillis()) / 60_000.0)
            .toLong()
            .coerceAtLeast(1)
}