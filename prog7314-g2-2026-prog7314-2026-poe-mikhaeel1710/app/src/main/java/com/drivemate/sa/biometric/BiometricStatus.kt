package com.drivemate.sa.biometric

import android.content.Context
import android.content.Intent
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricManager.Authenticators.BIOMETRIC_STRONG
import android.os.Build
import android.provider.Settings

private const val ALLOWED_AUTHENTICATORS =
    BIOMETRIC_STRONG
enum class BiometricStatusKind {
    AVAILABLE,
    NO_HARDWARE,
    HARDWARE_UNAVAILABLE,
    NONE_ENROLLED,
    SECURITY_UPDATE_REQUIRED,
    UNSUPPORTED,
    UNKNOWN
}

data class BiometricStatus(
    val kind: BiometricStatusKind,
    val message: String
) {
    val isAvailable: Boolean get() = kind == BiometricStatusKind.AVAILABLE

    /** Whether this row should offer a "Set up" link into system Settings. */
    val canBeFixedInSettings: Boolean
        get() = kind == BiometricStatusKind.NONE_ENROLLED ||
                kind == BiometricStatusKind.HARDWARE_UNAVAILABLE
}

object BiometricStatusChecker {

    fun check(context: Context): BiometricStatus {
        val biometricManager = BiometricManager.from(context)

        return when (biometricManager.canAuthenticate(ALLOWED_AUTHENTICATORS)) {
            BiometricManager.BIOMETRIC_SUCCESS ->
                BiometricStatus(
                    BiometricStatusKind.AVAILABLE,
                    "Biometric login is available on this device."
                )

            BiometricManager.BIOMETRIC_ERROR_NO_HARDWARE ->
                BiometricStatus(
                    BiometricStatusKind.NO_HARDWARE,
                    "This device doesn't have a biometric sensor."
                )

            BiometricManager.BIOMETRIC_ERROR_HW_UNAVAILABLE ->
                BiometricStatus(
                    BiometricStatusKind.HARDWARE_UNAVAILABLE,
                    "The biometric sensor is temporarily unavailable."
                )

            BiometricManager.BIOMETRIC_ERROR_NONE_ENROLLED ->
                BiometricStatus(
                    BiometricStatusKind.NONE_ENROLLED,
                    "No fingerprint or face is set up on this device."
                )

            BiometricManager.BIOMETRIC_ERROR_SECURITY_UPDATE_REQUIRED ->
                BiometricStatus(
                    BiometricStatusKind.SECURITY_UPDATE_REQUIRED,
                    "A security update is needed before biometrics can be used."
                )

            else ->
                BiometricStatus(
                    BiometricStatusKind.UNKNOWN,
                    "Biometric status couldn't be determined."
                )
        }
    }

    fun enrollIntent(): Intent {
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            Intent(Settings.ACTION_BIOMETRIC_ENROLL).putExtra(
                Settings.EXTRA_BIOMETRIC_AUTHENTICATORS_ALLOWED,
                ALLOWED_AUTHENTICATORS
            )
        } else {
            Intent(Settings.ACTION_SECURITY_SETTINGS)
        }
    }
}