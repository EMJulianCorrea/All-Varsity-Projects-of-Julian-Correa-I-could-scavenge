package com.drivemate.sa.ui.theme

import androidx.compose.ui.graphics.Color
import com.drivemate.sa.network.MaintenanceType

// DriveMate brand palette - light theme
val DriveMatePrimary = Color(0xFF2F4A47)
val DriveMateOnPrimary = Color(0xFFFFFFFF)

val DriveMatePrimaryContainer = Color(0xFFD6E8E4)
val DriveMateOnPrimaryContainer = Color(0xFF10201E)

val DriveMateSecondary = Color(0xFF596966)
val DriveMateOnSecondary = Color(0xFFFFFFFF)

val DriveMateSecondaryContainer = Color(0xFFDCE7E4)
val DriveMateOnSecondaryContainer = Color(0xFF17201F)

val DriveMateTertiary = Color(0xFF675A73)
val DriveMateOnTertiary = Color(0xFFFFFFFF)

val DriveMateBackground = Color(0xFFF6F8F7)
val DriveMateOnBackground = Color(0xFF191C1B)

val DriveMateSurface = Color(0xFFFFFFFF)
val DriveMateOnSurface = Color(0xFF191C1B)

val DriveMateSurfaceVariant = Color(0xFFE4E9E7)
val DriveMateOnSurfaceVariant = Color(0xFF424846)

val DriveMateOutline = Color(0xFF707976)


// DriveMate brand palette - dark theme
val DriveMateDarkPrimary = Color(0xFFAFCFC8)
val DriveMateDarkOnPrimary = Color(0xFF173732)

val DriveMateDarkPrimaryContainer = Color(0xFF2F4A47)
val DriveMateDarkOnPrimaryContainer = Color(0xFFD5E8E3)

val DriveMateDarkSecondary = Color(0xFFB9CBC6)
val DriveMateDarkOnSecondary = Color(0xFF253431)

val DriveMateDarkSecondaryContainer = Color(0xFF3A4946)
val DriveMateDarkOnSecondaryContainer = Color(0xFFD5E5E1)

val DriveMateDarkTertiary = Color(0xFFD3C0DA)
val DriveMateDarkOnTertiary = Color(0xFF382D40)

val DriveMateDarkBackground = Color(0xFF111413)
val DriveMateDarkOnBackground = Color(0xFFE1E4E2)

val DriveMateDarkSurface = Color(0xFF171A19)
val DriveMateDarkOnSurface = Color(0xFFE1E4E2)

val DriveMateDarkSurfaceVariant = Color(0xFF303634)
val DriveMateDarkOnSurfaceVariant = Color(0xFFC3CBC8)

val DriveMateDarkOutline = Color(0xFF8C9693)


// Maintenance badge colours
val ServiceYellow = Color(0xFFF4C430)
val RepairOrange = Color(0xFFE07A1F)
val InspectionBlue = Color(0xFF4C6EF5)
val PickUpGreen = Color(0xFF2FB380)
val OtherGrey = Color(0xFF777777)

fun MaintenanceType.badgeColor(): Color =
    when (this) {
        MaintenanceType.SERVICE -> ServiceYellow
        MaintenanceType.REPAIR -> RepairOrange
        MaintenanceType.INSPECTION -> InspectionBlue
        MaintenanceType.PICK_UP -> PickUpGreen
        MaintenanceType.OTHER -> OtherGrey
    }