package com.drivemate.sa.ui.screens

import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.MaintenanceRecord
import com.drivemate.sa.network.MaintenanceType
import com.drivemate.sa.ui.theme.DriveMateTheme
import java.time.LocalDate

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MaintenanceScreen(
    records: List<MaintenanceRecord>,
    onAddRecord: (MaintenanceRecord) -> Unit = {},
    onMarkCompleted: (MaintenanceRecord) -> Unit = {},
    onDeleteRecord: (String) -> Unit = {},
    onBack: () -> Unit = {}
) {
    var selectedTab by remember {
        mutableStateOf(0)
    }

    var showAddSheet by remember {
        mutableStateOf(false)
    }

    val today = LocalDate.now()

    val upcoming = records
        .filter { it.isUpcoming(today) }
        .sortedBy {
            it.nextDueDate ?: it.date
        }

    val history = records
        .filterNot { it.isUpcoming(today) }
        .sortedByDescending {
            it.date
        }

    Scaffold(
        containerColor = MaterialTheme.colorScheme.background,
        floatingActionButton = {
            FloatingActionButton(
                onClick = {
                    showAddSheet = true
                }
            ) {
                Icon(
                    imageVector = Icons.Filled.Add,
                    contentDescription = stringResource(
                        R.string.add_maintenance_record
                    )
                )
            }
        }
    ) { padding ->

        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
        ) {
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .padding(
                        horizontal = 20.dp,
                        vertical = 12.dp
                    ),
                horizontalArrangement =
                    Arrangement.SpaceBetween,
                verticalAlignment =
                    Alignment.CenterVertically
            ) {
                Text(
                    text = stringResource(
                        R.string.vehicle_health
                    ),
                    style =
                        MaterialTheme.typography
                            .headlineSmall
                )
                IconButton(
                    onClick = onBack
                ) {
                    Icon(
                        imageVector = Icons.Filled.ArrowBack,
                        contentDescription = stringResource(
                            R.string.back
                        ),
                        tint = MaterialTheme.colorScheme.onBackground
                    )
                }
            }

            TabRow(
                selectedTabIndex = selectedTab
            ) {
                Tab(
                    selected = selectedTab == 0,
                    onClick = {
                        selectedTab = 0
                    },
                    text = {
                        Text(
                            stringResource(
                                R.string.maintenance_upcoming_count,
                                upcoming.size
                            )
                        )
                    }
                )

                Tab(
                    selected = selectedTab == 1,
                    onClick = {
                        selectedTab = 1
                    },
                    text = {
                        Text(
                            stringResource(
                                R.string.maintenance_history_count,
                                history.size
                            )
                        )
                    }
                )
            }

            val visibleRecords =
                if (selectedTab == 0) {
                    upcoming
                } else {
                    history
                }

            if (visibleRecords.isEmpty()) {

                Box(
                    modifier = Modifier.fillMaxSize(),
                    contentAlignment =
                        Alignment.Center
                ) {
                    Text(
                        text =
                            if (selectedTab == 0) {
                                stringResource(
                                    R.string.no_upcoming_maintenance
                                )
                            } else {
                                stringResource(
                                    R.string.no_service_history
                                )
                            },
                        color =
                            MaterialTheme.colorScheme
                                .onSurfaceVariant
                    )
                }

            } else {

                LazyColumn(
                    contentPadding = PaddingValues(
                        horizontal = 20.dp,
                        vertical = 12.dp
                    ),
                    verticalArrangement =
                        Arrangement.spacedBy(10.dp)
                ) {
                    items(
                        visibleRecords,
                        key = {
                            it.id
                        }
                    ) { record ->

                        val dismissState =
                            rememberSwipeToDismissBoxState(
                                confirmValueChange = { value ->

                                    if (
                                        value !=
                                        SwipeToDismissBoxValue
                                            .Settled
                                    ) {
                                        onDeleteRecord(
                                            record.id
                                        )

                                        true
                                    } else {
                                        false
                                    }
                                }
                            )

                        SwipeToDismissBox(
                            state = dismissState,
                            backgroundContent = {
                                Box(
                                    modifier = Modifier
                                        .fillMaxSize()
                                        .clip(
                                            RoundedCornerShape(
                                                16.dp
                                            )
                                        )
                                        .background(
                                            MaterialTheme
                                                .colorScheme
                                                .errorContainer
                                        )
                                        .padding(
                                            horizontal = 20.dp
                                        ),
                                    contentAlignment =
                                        Alignment.CenterEnd
                                ) {
                                    Icon(
                                        imageVector =
                                            Icons.Filled.Delete,
                                        contentDescription =
                                            stringResource(
                                                R.string.delete
                                            ),
                                        tint =
                                            MaterialTheme
                                                .colorScheme
                                                .onErrorContainer
                                    )
                                }
                            }
                        ) {
                            MaintenanceRecordCard(
                                record = record,
                                onClick = {
                                    if (
                                        selectedTab == 0
                                    ) {
                                        onMarkCompleted(
                                            record
                                        )
                                    }
                                }
                            )
                        }
                    }

                    item {
                        Spacer(
                            modifier =
                                Modifier.height(72.dp)
                        )
                    }
                }
            }
        }
    }

    if (showAddSheet) {
        AddMaintenanceRecordSheet(
            onDismiss = {
                showAddSheet = false
            },
            onSave = { record ->
                onAddRecord(record)
                showAddSheet = false
            }
        )
    }
}

@Preview(showBackground = true)
@Composable
private fun MaintenanceScreenPreview() {
    val sample = listOf(
        MaintenanceRecord(
            type = MaintenanceType.SERVICE,
            title = "Oil Change",
            date = LocalDate.now()
                .minusDays(30),
            mileageKm = 82000,
            cost = 950.0,
            nextDueDate = LocalDate.now()
                .plusDays(15),
            completed = true
        ),
        MaintenanceRecord(
            type = MaintenanceType.REPAIR,
            title = "Wheel Alignment",
            date = LocalDate.now()
                .minusDays(10),
            mileageKm = 83200,
            cost = 450.0,
            completed = true
        ),
        MaintenanceRecord(
            type = MaintenanceType.INSPECTION,
            title = "Licence Disc Renewal",
            date = LocalDate.now()
                .plusDays(20),
            nextDueDate = LocalDate.now()
                .plusDays(20),
            completed = false
        )
    )

    DriveMateTheme {
        MaintenanceScreen(
            records = sample
        )
    }
}
