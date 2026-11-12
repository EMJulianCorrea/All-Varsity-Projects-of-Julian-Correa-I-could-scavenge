package com.drivemate.sa.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Warning
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.MaintenanceRecord
import com.drivemate.sa.network.MaintenanceType
import com.drivemate.sa.ui.theme.badgeColor
import java.time.format.DateTimeFormatter

@Composable
fun MaintenanceRecordCard(
    record: MaintenanceRecord,
    onClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    val dateFormatter =
        DateTimeFormatter.ofPattern("d MMM")

    val displayDate =
        record.nextDueDate?.let {
            stringResource(
                R.string.maintenance_due_date,
                it.format(dateFormatter)
            )
        } ?: record.date.format(dateFormatter)

    Row(
        modifier = modifier
            .fillMaxWidth()
            .clip(
                RoundedCornerShape(16.dp)
            )
            .background(
                MaterialTheme.colorScheme.surfaceVariant
            )
            .clickable(
                onClick = onClick
            )
            .padding(
                horizontal = 16.dp,
                vertical = 14.dp
            ),
        verticalAlignment =
            Alignment.CenterVertically
    ) {
        Column(
            modifier =
                Modifier.weight(1f)
        ) {
            StatusBadge(
                record.type
            )

            Spacer(
                modifier =
                    Modifier.height(6.dp)
            )

            Text(
                text =
                    record.title,
                fontWeight =
                    FontWeight.SemiBold,
                style =
                    MaterialTheme.typography.bodyLarge
            )

            Text(
                text =
                    displayDate,
                style =
                    MaterialTheme.typography.bodySmall,
                color =
                    MaterialTheme.colorScheme
                        .onSurfaceVariant
            )
        }

        record.cost?.let { cost ->
            Text(
                text = stringResource(
                    R.string.maintenance_cost_value,
                    cost
                ),
                fontWeight =
                    FontWeight.Medium
            )
        }

        if (
            record.isOverdue()
        ) {
            Spacer(
                modifier =
                    Modifier.width(8.dp)
            )

            Icon(
                imageVector =
                    Icons.Filled.Warning,
                contentDescription =
                    stringResource(
                        R.string.maintenance_overdue
                    ),
                tint =
                    MaterialTheme.colorScheme.error
            )
        }
    }
}

@Composable
private fun StatusBadge(
    type: MaintenanceType
) {
    Box(
        modifier = Modifier
            .clip(
                RoundedCornerShape(50)
            )
            .background(
                type.badgeColor()
            )
            .padding(
                horizontal = 10.dp,
                vertical = 2.dp
            )
    ) {
        Text(
            text =
                maintenanceTypeLabel(
                    type
                ),
            color =
                Color.White,
            style =
                MaterialTheme.typography.labelSmall
        )
    }
}

@Composable
private fun maintenanceTypeLabel(
    type: MaintenanceType
): String {
    return when (type) {
        MaintenanceType.SERVICE ->
            stringResource(
                R.string.maintenance_type_service
            )

        MaintenanceType.REPAIR ->
            stringResource(
                R.string.maintenance_type_repair
            )

        MaintenanceType.INSPECTION ->
            stringResource(
                R.string.maintenance_type_inspection
            )

        MaintenanceType.PICK_UP ->
            stringResource(
                R.string.maintenance_type_pick_up
            )

        MaintenanceType.OTHER ->
            stringResource(
                R.string.maintenance_type_other
            )
    }
}