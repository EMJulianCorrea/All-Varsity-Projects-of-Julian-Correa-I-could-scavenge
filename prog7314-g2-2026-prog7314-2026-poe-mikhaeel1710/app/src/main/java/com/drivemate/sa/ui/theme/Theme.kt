package com.drivemate.sa.ui.theme

import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.dynamicDarkColorScheme
import androidx.compose.material3.dynamicLightColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.graphics.Color

private val DarkColorScheme = darkColorScheme(
    primary = DriveMateDarkPrimary,
    onPrimary = DriveMateDarkOnPrimary,

    primaryContainer = DriveMateDarkPrimaryContainer,
    onPrimaryContainer = DriveMateDarkOnPrimaryContainer,

    secondary = DriveMateDarkSecondary,
    onSecondary = DriveMateDarkOnSecondary,

    secondaryContainer = DriveMateDarkSecondaryContainer,
    onSecondaryContainer = DriveMateDarkOnSecondaryContainer,

    tertiary = DriveMateDarkTertiary,
    onTertiary = DriveMateDarkOnTertiary,

    background = DriveMateDarkBackground,
    onBackground = DriveMateDarkOnBackground,

    surface = DriveMateDarkSurface,
    onSurface = DriveMateDarkOnSurface,

    surfaceVariant = DriveMateDarkSurfaceVariant,
    onSurfaceVariant = DriveMateDarkOnSurfaceVariant,

    outline = DriveMateDarkOutline,

    error = Color(0xFFFFB4AB),
    onError = Color(0xFF690005)
)

private val LightColorScheme = lightColorScheme(
    primary = DriveMatePrimary,
    onPrimary = DriveMateOnPrimary,

    primaryContainer = DriveMatePrimaryContainer,
    onPrimaryContainer = DriveMateOnPrimaryContainer,

    secondary = DriveMateSecondary,
    onSecondary = DriveMateOnSecondary,

    secondaryContainer = DriveMateSecondaryContainer,
    onSecondaryContainer = DriveMateOnSecondaryContainer,

    tertiary = DriveMateTertiary,
    onTertiary = DriveMateOnTertiary,

    background = DriveMateBackground,
    onBackground = DriveMateOnBackground,

    surface = DriveMateSurface,
    onSurface = DriveMateOnSurface,

    surfaceVariant = DriveMateSurfaceVariant,
    onSurfaceVariant = DriveMateOnSurfaceVariant,

    outline = DriveMateOutline,

    error = Color(0xFFBA1A1A),
    onError = Color.White
)

@Composable
fun DriveMateTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),

    // DriveMate uses its own palette so the UI remains consistent
    // across different Android devices and wallpapers.
    dynamicColor: Boolean = false,

    content: @Composable () -> Unit
) {
    val colorScheme =
        when {
            dynamicColor &&
                    Build.VERSION.SDK_INT >=
                    Build.VERSION_CODES.S -> {

                val context =
                    LocalContext.current

                if (darkTheme) {
                    dynamicDarkColorScheme(context)
                } else {
                    dynamicLightColorScheme(context)
                }
            }

            darkTheme ->
                DarkColorScheme

            else ->
                LightColorScheme
        }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography
    ) {
        Surface(
            modifier =
                Modifier.fillMaxSize(),

            color =
                MaterialTheme.colorScheme.background
        ) {
            content()
        }
    }
}