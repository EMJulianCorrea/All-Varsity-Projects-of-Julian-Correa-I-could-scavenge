package com.drivemate.sa.ui.screens

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.MaintenanceRecord
import com.drivemate.sa.network.MaintenanceType
import java.time.LocalDate

@OptIn(
    ExperimentalMaterial3Api::class,
    ExperimentalLayoutApi::class
)
@Composable
fun AddMaintenanceRecordSheet(
    onDismiss: () -> Unit,
    onSave: (MaintenanceRecord) -> Unit
) {
    var selectedType by remember {
        mutableStateOf(
            MaintenanceType.SERVICE
        )
    }

    var title by remember {
        mutableStateOf("")
    }

    var mileage by remember {
        mutableStateOf("")
    }

    var cost by remember {
        mutableStateOf("")
    }

    var notes by remember {
        mutableStateOf("")
    }

    var isReminder by remember {
        mutableStateOf(false)
    }

    val selectedTypeLabel =
        maintenanceTypeLabel(
            selectedType
        )

    ModalBottomSheet(
        onDismissRequest = onDismiss
    ) {
        Column(
            modifier = Modifier
                .fillMaxWidth()
                .verticalScroll(
                    rememberScrollState()
                )
                .imePadding()
                .navigationBarsPadding()
                .padding(
                    horizontal = 20.dp
                )
                .padding(
                    bottom = 24.dp
                )
        ) {
            Text(
                text = stringResource(
                    R.string.add_maintenance_record
                ),
                style =
                    MaterialTheme.typography.titleLarge
            )

            Spacer(
                modifier =
                    Modifier.height(16.dp)
            )

            Text(
                text = stringResource(
                    R.string.maintenance_type
                ),
                style =
                    MaterialTheme.typography.labelMedium
            )

            Spacer(
                modifier =
                    Modifier.height(8.dp)
            )

            FlowRow(
                modifier =
                    Modifier.fillMaxWidth(),

                horizontalArrangement =
                    Arrangement.spacedBy(8.dp),

                verticalArrangement =
                    Arrangement.spacedBy(8.dp)
            ) {
                MaintenanceType.values()
                    .forEach { type ->

                        FilterChip(
                            selected =
                                selectedType == type,

                            onClick = {
                                selectedType = type
                            },

                            label = {
                                Text(
                                    maintenanceTypeLabel(
                                        type
                                    )
                                )
                            }
                        )
                    }
            }

            Spacer(
                modifier =
                    Modifier.height(16.dp)
            )

            OutlinedTextField(
                value = title,

                onValueChange = {
                    title = it
                },

                label = {
                    Text(
                        stringResource(
                            R.string.maintenance_title
                        )
                    )
                },

                modifier =
                    Modifier.fillMaxWidth(),

                singleLine = true
            )

            Spacer(
                modifier =
                    Modifier.height(12.dp)
            )

            Row(
                modifier =
                    Modifier.fillMaxWidth(),

                horizontalArrangement =
                    Arrangement.spacedBy(12.dp)
            ) {
                OutlinedTextField(
                    value = mileage,

                    onValueChange = {
                        mileage =
                            it.filter(
                                Char::isDigit
                            )
                    },

                    label = {
                        Text(
                            stringResource(
                                R.string.maintenance_mileage
                            )
                        )
                    },

                    modifier =
                        Modifier.weight(1f),

                    singleLine = true
                )

                OutlinedTextField(
                    value = cost,

                    onValueChange = {
                        cost =
                            it.filter { character ->
                                character.isDigit() ||
                                        character == '.'
                            }
                    },

                    label = {
                        Text(
                            stringResource(
                                R.string.maintenance_cost
                            )
                        )
                    },

                    modifier =
                        Modifier.weight(1f),

                    singleLine = true
                )
            }

            Spacer(
                modifier =
                    Modifier.height(12.dp)
            )

            OutlinedTextField(
                value = notes,

                onValueChange = {
                    notes = it
                },

                label = {
                    Text(
                        stringResource(
                            R.string.maintenance_notes
                        )
                    )
                },

                modifier =
                    Modifier.fillMaxWidth(),

                minLines = 3,
                maxLines = 4
            )

            Spacer(
                modifier =
                    Modifier.height(16.dp)
            )

            Row(
                modifier =
                    Modifier.fillMaxWidth(),

                verticalAlignment =
                    Alignment.CenterVertically
            ) {
                Checkbox(
                    checked =
                        isReminder,

                    onCheckedChange = {
                        isReminder = it
                    }
                )

                Spacer(
                    modifier =
                        Modifier.width(8.dp)
                )

                Text(
                    text = stringResource(
                        R.string.maintenance_upcoming_reminder
                    ),

                    modifier =
                        Modifier.weight(1f),

                    style =
                        MaterialTheme.typography.bodyMedium
                )
            }

            Spacer(
                modifier =
                    Modifier.height(20.dp)
            )

            Button(
                onClick = {
                    val today =
                        LocalDate.now()

                    onSave(
                        MaintenanceRecord(
                            type =
                                selectedType,

                            title =
                                title.ifBlank {
                                    selectedTypeLabel
                                },

                            date =
                                today,

                            mileageKm =
                                mileage.toIntOrNull(),

                            cost =
                                cost.toDoubleOrNull(),

                            notes =
                                notes,

                            nextDueDate =
                                if (isReminder) {
                                    today.plusDays(30)
                                } else {
                                    null
                                },

                            completed =
                                !isReminder
                        )
                    )
                },

                enabled =
                    title.isNotBlank(),

                modifier =
                    Modifier.fillMaxWidth()
            ) {
                Text(
                    stringResource(
                        R.string.save
                    )
                )
            }

            Spacer(
                modifier =
                    Modifier.height(12.dp)
            )
        }
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