package com.drivemate.sa.notifications

import android.content.Context

/** Stores whether the user has switched DriveMate reminders on. */
object NotificationPreferences {

    private const val PREFS_NAME = "drivemate_notifications"
    private const val KEY_ENABLED = "reminders_enabled"

    fun isEnabled(context: Context): Boolean =
        context.applicationContext
            .getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .getBoolean(KEY_ENABLED, false)

    fun setEnabled(context: Context, enabled: Boolean) {
        context.applicationContext
            .getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
            .edit()
            .putBoolean(KEY_ENABLED, enabled)
            .apply()
    }
}
