package com.drivemate.sa.ui.screens

import androidx.compose.foundation.layout.navigationBarsPadding
import android.Manifest
import android.os.Build
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.material3.Switch
import com.drivemate.sa.notifications.NotificationPreferences
import com.drivemate.sa.notifications.RenewalReminderWorker
import com.drivemate.sa.notifications.SecureNotificationHelper
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.biometric.BiometricManager
import androidx.biometric.BiometricPrompt
import androidx.core.content.ContextCompat
import androidx.fragment.app.FragmentActivity
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import android.annotation.SuppressLint
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.Lifecycle
import androidx.lifecycle.compose.LifecycleEventEffect
import com.drivemate.sa.R
import com.drivemate.sa.biometric.BiometricStatus
import com.drivemate.sa.biometric.BiometricStatusChecker
import com.drivemate.sa.locale.LanguageManager
import com.google.firebase.auth.FirebaseUser

private val AccentDark = Color(0xFF2F4A47)

@Composable
fun SettingScreen(
    currentUser: FirebaseUser?,
    onBackClick: () -> Unit,
    onViewFullProfile: () -> Unit,
    onMyBookingsClick: () -> Unit,
    onMyMessagesClick: () -> Unit,
    onHelpCenterClick: () -> Unit,
    onAppFeedbackClick: () -> Unit,
    onLanguageClick: () -> Unit,
    onNotificationClick: () -> Unit
) {
    val context = LocalContext.current
    val fragmentActivity =
        context as? FragmentActivity
    val biometricSuccessText =
        stringResource(
            R.string.biometric_auth_success
        )

    val biometricFailedText =
        stringResource(
            R.string.biometric_not_recognised
        )

    val biometricPromptTitle =
        stringResource(
            R.string.biometric_prompt_title
        )

    val biometricPromptSubtitle =
        stringResource(
            R.string.biometric_prompt_subtitle
        )

    val biometricUnavailableText =
        stringResource(
            R.string.biometric_auth_unavailable
        )

    val biometricCancelText =
        stringResource(
            R.string.cancel
        )

    var biometricAuthMessage by remember {
        mutableStateOf<String?>(null)
    }

    val biometricExecutor = remember(context) {
        ContextCompat.getMainExecutor(context)
    }

    val biometricPrompt = remember(
        fragmentActivity,
        biometricSuccessText,
        biometricFailedText
    ) {
        fragmentActivity?.let { activity ->

            BiometricPrompt(
                activity,
                biometricExecutor,
                object :
                    BiometricPrompt.AuthenticationCallback() {

                    override fun onAuthenticationSucceeded(
                        result:
                        BiometricPrompt.AuthenticationResult
                    ) {
                        super.onAuthenticationSucceeded(result)

                        biometricAuthMessage =
                            biometricSuccessText
                    }

                    override fun onAuthenticationFailed() {
                        super.onAuthenticationFailed()

                        biometricAuthMessage =
                            biometricFailedText
                    }

                    override fun onAuthenticationError(
                        errorCode: Int,
                        errString: CharSequence
                    ) {
                        super.onAuthenticationError(
                            errorCode,
                            errString
                        )

                        biometricAuthMessage =
                            errString.toString()
                    }
                }
            )
        }
    }

    val biometricPromptInfo = remember(
        biometricPromptTitle,
        biometricPromptSubtitle,
        biometricCancelText
    ) {
        BiometricPrompt.PromptInfo.Builder()
            .setTitle(
                biometricPromptTitle
            )
            .setSubtitle(
                biometricPromptSubtitle
            )
            .setAllowedAuthenticators(
                BiometricManager.Authenticators.BIOMETRIC_STRONG
            )
            .setNegativeButtonText(
                biometricCancelText
            )
            .build()
    }
    var showLanguageDialog by remember { mutableStateOf(false) }
    val currentLanguageTag =
        LanguageManager.currentLanguageTag(context)

    val currentLanguageName =
        LanguageManager.currentLanguageDisplayName(context)
    var biometricStatus by remember { mutableStateOf(BiometricStatusChecker.check(context)) }
    // Re-check whenever this screen comes back to the foreground, so
    // enrolling a fingerprint in system Settings and returning here
    // updates the row without needing a manual refresh.
    LifecycleEventEffect(Lifecycle.Event.ON_RESUME) {
        biometricStatus = BiometricStatusChecker.check(context)
    }

    var remindersEnabled by remember {
        mutableStateOf(
            NotificationPreferences.isEnabled(context) &&
                    SecureNotificationHelper.hasPermission(context)
        )
    }
    var notificationMessage by remember { mutableStateOf<String?>(null) }
    val permissionDeniedText =
        stringResource(R.string.notification_permission_denied)

    fun applyReminders(enabled: Boolean) {
        NotificationPreferences.setEnabled(context, enabled)
        remindersEnabled = enabled
        if (enabled) {
            SecureNotificationHelper.createChannel(context)
            RenewalReminderWorker.schedule(context)
        } else {
            RenewalReminderWorker.cancel(context)
        }
    }

    val notificationPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestPermission()
    ) { granted ->
        if (granted) {
            notificationMessage = null
            applyReminders(true)
        } else {
            applyReminders(false)
            notificationMessage = permissionDeniedText
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .navigationBarsPadding()
            .padding(horizontal = 24.dp, vertical = 20.dp)
    ) {
        // Back chevron
        IconButton(
            onClick = onBackClick
        ) {
            Icon(
                imageVector = Icons.Filled.ArrowBack,
                contentDescription = stringResource(
                    R.string.back
                ),
                tint = MaterialTheme.colorScheme.onBackground
            )
        }

        Spacer(modifier = Modifier.height(16.dp))

        Text(
            text = stringResource(R.string.account_title),
            style = MaterialTheme.typography.headlineLarge,
            fontWeight = FontWeight.Bold
        )

        Spacer(modifier = Modifier.height(24.dp))

        // Profile row: avatar + email/name
        Row(
            verticalAlignment = Alignment.CenterVertically
        ) {
            ProfileAvatar(currentUser)

            Spacer(modifier = Modifier.width(12.dp))

            Text(
                text = currentUser?.email
                    ?: currentUser?.displayName
                    ?: stringResource(R.string.not_signed_in),
                style = MaterialTheme.typography.bodyLarge
            )
        }

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = onViewFullProfile,
            modifier = Modifier.fillMaxWidth(),
            shape = RoundedCornerShape(28.dp),
            colors = ButtonDefaults.buttonColors(
                containerColor = AccentDark,
                contentColor = Color.White
            )
        ) {
            Text(
                text = stringResource(R.string.view_full_profile),
                fontWeight = FontWeight.Medium
            )
        }

        Spacer(modifier = Modifier.height(32.dp))

        SettingsRow(title = stringResource(R.string.my_bookings), onClick = onMyBookingsClick)
        HorizontalDivider()
        SettingsRow(title = stringResource(R.string.my_messages), onClick = onMyMessagesClick)

        Spacer(modifier = Modifier.height(28.dp))

        SectionHeader(title = stringResource(R.string.support_header))
        SettingsRow(title = stringResource(R.string.help_center), onClick = onHelpCenterClick)
        HorizontalDivider()
        SettingsRow(title = stringResource(R.string.app_feedback), onClick = onAppFeedbackClick)

        Spacer(modifier = Modifier.height(28.dp))

        SectionHeader(title = stringResource(R.string.preferences_header))

        // Language row: opens the picker AND still calls the passed-in
        // callback, in case the screen owner wants to log/react to the tap.
        SettingsRow(
            title = "${stringResource(R.string.language_label)} ($currentLanguageName)",
            onClick = {
                showLanguageDialog = true
                onLanguageClick()
            }
        )
        NotificationToggleRow(
            title = stringResource(R.string.notification_label),
            subtitle = if (remindersEnabled) {
                stringResource(R.string.notification_reminders_on)
            } else {
                stringResource(R.string.notification_reminders_off)
            },
            checked = remindersEnabled,
            onCheckedChange = { wantsOn ->
                onNotificationClick()
                notificationMessage = null

                if (!wantsOn) {
                    applyReminders(false)
                } else if (
                    Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU &&
                    !SecureNotificationHelper.hasPermission(context)
                ) {
                    notificationPermissionLauncher.launch(
                        Manifest.permission.POST_NOTIFICATIONS
                    )
                } else if (!SecureNotificationHelper.hasPermission(context)) {
                    notificationMessage = permissionDeniedText
                } else {
                    applyReminders(true)
                }
            }
        )

        notificationMessage?.let { message ->
            Text(
                text = message,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.error
            )
        }

        Spacer(modifier = Modifier.height(28.dp))

        SectionHeader(title = stringResource(R.string.security_header))
        BiometricStatusRow(
            status = biometricStatus,

            onFixClick = {
                context.startActivity(
                    BiometricStatusChecker.enrollIntent()
                )
            },

            onAuthenticateClick = {

                biometricAuthMessage = null

                if (biometricPrompt != null) {
                    biometricPrompt.authenticate(
                        biometricPromptInfo
                    )
                } else {
                    biometricAuthMessage =
                        biometricUnavailableText
                }
            }
        )

        biometricAuthMessage?.let { message ->

            Spacer(
                modifier = Modifier.height(8.dp)
            )

            Text(
                text = message,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.primary
            )

        }
        Spacer(
            modifier = Modifier.height(32.dp))
    }

    if (showLanguageDialog) {
        LanguagePickerDialog(
            currentTag = currentLanguageTag,
            onDismiss = { showLanguageDialog = false },
            onLanguageSelected = { tag ->
                LanguageManager.setLanguage(
                    context,
                    tag
                )

                showLanguageDialog = false
            }
        )
    }
}

/**
 * Circular avatar placeholder showing the user's initial. Swap the Box
 * content for Coil's AsyncImage(model = currentUser?.photoUrl, ...) if/when
 * the project adds a Coil dependency, to show the real profile photo.
 */
@Composable
private fun ProfileAvatar(currentUser: FirebaseUser?) {
    val label = (currentUser?.displayName ?: currentUser?.email)
        ?.trim()
        ?.firstOrNull()
        ?.uppercaseChar()
        ?.toString()
        ?: "?"

    Box(
        modifier = Modifier
            .size(48.dp)
            .background(MaterialTheme.colorScheme.primaryContainer, CircleShape),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = label,
            style = MaterialTheme.typography.titleMedium,
            color = MaterialTheme.colorScheme.onPrimaryContainer,
            fontWeight = FontWeight.Bold
        )
    }
}

@Composable
private fun SectionHeader(title: String) {
    Text(
        text = title,
        style = MaterialTheme.typography.titleMedium,
        fontWeight = FontWeight.Bold
    )
    Spacer(modifier = Modifier.height(8.dp))
}

@Composable
private fun SettingsRow(title: String, onClick: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .padding(vertical = 14.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Text(
            text = title,
            style = MaterialTheme.typography.bodyLarge
        )
        Text(
            text = "\u2197", // ↗
            fontSize = 18.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
    }
}

@Composable
private fun NotificationToggleRow(
    title: String,
    subtitle: String,
    checked: Boolean,
    onCheckedChange: (Boolean) -> Unit
) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 14.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = title,
                style = MaterialTheme.typography.bodyLarge
            )
            Spacer(modifier = Modifier.height(2.dp))
            Text(
                text = subtitle,
                style = MaterialTheme.typography.bodySmall,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        Switch(
            checked = checked,
            onCheckedChange = onCheckedChange
        )
    }
}

/**
 * Informational row - not a toggle. Shows the device's current biometric
 * state, and (only when it's fixable) a "Set up" link into system Settings.
 */
@Composable
private fun BiometricStatusRow(
    status: BiometricStatus,
    onFixClick: () -> Unit,
    onAuthenticateClick: () -> Unit
){
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 14.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = stringResource(R.string.biometric_login_label),
                style = MaterialTheme.typography.bodyLarge
            )
            Spacer(modifier = Modifier.height(2.dp))
            Text(
                text = status.message,
                style = MaterialTheme.typography.bodySmall,
                color = if (status.isAvailable) {
                    MaterialTheme.colorScheme.onSurfaceVariant
                } else {
                    MaterialTheme.colorScheme.error
                }
            )

        }

        when {
            status.canBeFixedInSettings -> {
                Text(
                    text = stringResource(R.string.biometric_set_up_action),
                    style = MaterialTheme.typography.labelLarge,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.clickable(onClick = onFixClick)
                )
            }

            status.isAvailable -> {
                Text(
                    text = stringResource(
                        R.string.biometric_authenticate_action
                    ),
                    style = MaterialTheme.typography.labelLarge,
                    color = MaterialTheme.colorScheme.primary,
                    modifier = Modifier.clickable(
                        onClick = onAuthenticateClick
                    )
                )
                Spacer(
                    modifier = Modifier.height(32.dp)
                )}

        }
    }
}