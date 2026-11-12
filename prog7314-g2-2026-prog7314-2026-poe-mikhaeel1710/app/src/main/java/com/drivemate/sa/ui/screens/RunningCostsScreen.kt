package com.drivemate.sa.ui.screens

import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.Checkbox
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.CreateExpenseRequest
import com.drivemate.sa.network.CreateFuelEntryRequest
import com.drivemate.sa.network.Expense
import com.drivemate.sa.network.FuelEntry
import com.drivemate.sa.network.Vehicle
import com.drivemate.sa.runningcosts.RunningCostsCalculator
import java.text.NumberFormat
import java.time.LocalDate
import java.util.Locale

@Composable
fun RunningCostsScreen(
    vehicles: List<Vehicle>,
    selectedVehicleId: Int?,
    fuelEntries: List<FuelEntry>,
    expenses: List<Expense>,
    onSelectVehicle: (Int) -> Unit,
    onAddFuelEntry: (CreateFuelEntryRequest) -> Unit,
    onAddExpense: (CreateExpenseRequest) -> Unit,
    onBack: () -> Unit
) {
    var showFuelDialog by remember {
        mutableStateOf(false)
    }

    var showExpenseDialog by remember {
        mutableStateOf(false)
    }

    var vehicleMenuExpanded by remember {
        mutableStateOf(false)
    }

    val selectedVehicle =
        vehicles.firstOrNull {
            it.id == selectedVehicleId
        } ?: vehicles.firstOrNull()

    val selectedFuelEntries =
        selectedVehicle?.let { vehicle ->
            fuelEntries.filter {
                it.vehicleId == vehicle.id
            }
        } ?: emptyList()

    val selectedExpenses =
        selectedVehicle?.let { vehicle ->
            expenses.filter {
                it.vehicleId == vehicle.id
            }
        } ?: emptyList()

    val overview =
        RunningCostsCalculator.buildOverview(
            fuelEntries = selectedFuelEntries,
            expenses = selectedExpenses
        )

    val currencyFormatter =
        NumberFormat.getCurrencyInstance(
            Locale.forLanguageTag("en-ZA")
        )

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(
                rememberScrollState()
            )
            .navigationBarsPadding()
            .padding(16.dp)
    ) {
        Row(
            modifier =
                Modifier.fillMaxWidth(),
            horizontalArrangement =
                Arrangement.SpaceBetween,
            verticalAlignment =
                Alignment.CenterVertically
        ) {
            Text(
                text = stringResource(
                    R.string.running_costs
                ),
                style =
                    MaterialTheme.typography
                        .headlineMedium
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

        Spacer(
            modifier = Modifier.height(16.dp)
        )

        if (vehicles.isEmpty()) {
            Text(
                text = stringResource(
                    R.string.running_costs_no_vehicle
                ),
                style =
                    MaterialTheme.typography.bodyLarge
            )

            return@Column
        }

        Text(
            text = stringResource(
                R.string.vehicle_label
            ),
            style =
                MaterialTheme.typography.labelLarge
        )

        Spacer(
            modifier = Modifier.height(6.dp)
        )

        Box {
            OutlinedButton(
                onClick = {
                    vehicleMenuExpanded = true
                }
            ) {
                Text(
                    selectedVehicle?.let {
                        "${it.nickname} - " +
                                it.registrationNumber
                    } ?: stringResource(
                        R.string.select_vehicle
                    )
                )
            }

            DropdownMenu(
                expanded =
                    vehicleMenuExpanded,
                onDismissRequest = {
                    vehicleMenuExpanded = false
                }
            ) {
                vehicles.forEach { vehicle ->
                    DropdownMenuItem(
                        text = {
                            Text(
                                "${vehicle.nickname} - " +
                                        vehicle.registrationNumber
                            )
                        },
                        onClick = {
                            vehicleMenuExpanded = false
                            onSelectVehicle(
                                vehicle.id
                            )
                        }
                    )
                }
            }
        }

        Spacer(
            modifier = Modifier.height(20.dp)
        )

        OverviewCard(
            title = stringResource(
                R.string.total_running_cost
            ),
            value =
                currencyFormatter.format(
                    overview.totalRunningCost
                )
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        OverviewCard(
            title = stringResource(
                R.string.fuel_spend
            ),
            value =
                currencyFormatter.format(
                    overview.totalFuelExpenditure
                )
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        OverviewCard(
            title = stringResource(
                R.string.other_expenses
            ),
            value =
                currencyFormatter.format(
                    overview.totalExpenseAmount
                )
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        OverviewCard(
            title = stringResource(
                R.string.fuel_purchased
            ),
            value = stringResource(
                R.string.litres_value,
                overview.totalLitresPurchased
            )
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        OverviewCard(
            title = stringResource(
                R.string.average_fuel_price
            ),
            value =
                overview.averagePricePerLitre
                    ?.let {
                        stringResource(
                            R.string.price_per_litre_value,
                            currencyFormatter.format(it)
                        )
                    }
                    ?: stringResource(
                        R.string.not_enough_data
                    )
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        OverviewCard(
            title = stringResource(
                R.string.estimated_consumption
            ),
            value =
                overview
                    .estimatedConsumptionLPer100Km
                    ?.let {
                        stringResource(
                            R.string.consumption_value,
                            it
                        )
                    }
                    ?: stringResource(
                        R.string.not_enough_data
                    )
        )

        Spacer(
            modifier = Modifier.height(24.dp)
        )

        Row(
            modifier =
                Modifier.fillMaxWidth(),
            horizontalArrangement =
                Arrangement.spacedBy(12.dp)
        ) {
            Button(
                onClick = {
                    showFuelDialog = true
                },
                modifier =
                    Modifier.weight(1f)
            ) {
                Text(
                    stringResource(
                        R.string.add_fuel
                    )
                )
            }

            Button(
                onClick = {
                    showExpenseDialog = true
                },
                modifier =
                    Modifier.weight(1f)
            ) {
                Text(
                    stringResource(
                        R.string.add_expense
                    )
                )
            }
        }

        Spacer(
            modifier = Modifier.height(28.dp)
        )

        Text(
            text = stringResource(
                R.string.fuel_history
            ),
            style =
                MaterialTheme.typography.titleLarge
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        if (
            selectedFuelEntries.isEmpty()
        ) {
            Text(
                stringResource(
                    R.string.no_fuel_entries
                )
            )
        } else {
            selectedFuelEntries
                .sortedByDescending {
                    it.date
                }
                .forEach { entry ->

                    FuelEntryCard(
                        entry = entry,
                        currencyFormatter =
                            currencyFormatter
                    )

                    Spacer(
                        modifier =
                            Modifier.height(8.dp)
                    )
                }
        }

        Spacer(
            modifier = Modifier.height(24.dp)
        )

        Text(
            text = stringResource(
                R.string.expense_history
            ),
            style =
                MaterialTheme.typography.titleLarge
        )

        Spacer(
            modifier = Modifier.height(10.dp)
        )

        if (
            selectedExpenses.isEmpty()
        ) {
            Text(
                stringResource(
                    R.string.no_expenses
                )
            )
        } else {
            selectedExpenses
                .sortedByDescending {
                    it.date
                }
                .forEach { expense ->

                    ExpenseCard(
                        expense = expense,
                        currencyFormatter =
                            currencyFormatter
                    )

                    Spacer(
                        modifier =
                            Modifier.height(8.dp)
                    )
                }
        }
    }

    if (
        showFuelDialog &&
        selectedVehicle != null
    ) {
        AddFuelDialog(
            vehicleId =
                selectedVehicle.id,

            onDismiss = {
                showFuelDialog = false
            },

            onSave = { request ->
                onAddFuelEntry(request)
                showFuelDialog = false
            }
        )
    }

    if (
        showExpenseDialog &&
        selectedVehicle != null
    ) {
        AddExpenseDialog(
            vehicleId =
                selectedVehicle.id,

            onDismiss = {
                showExpenseDialog = false
            },

            onSave = { request ->
                onAddExpense(request)
                showExpenseDialog = false
            }
        )
    }
}

@Composable
private fun OverviewCard(
    title: String,
    value: String
) {
    Card(
        modifier =
            Modifier.fillMaxWidth()
    ) {
        Column(
            modifier =
                Modifier.padding(16.dp)
        ) {
            Text(
                text = title,
                style =
                    MaterialTheme.typography
                        .labelLarge
            )

            Spacer(
                modifier = Modifier.height(4.dp)
            )

            Text(
                text = value,
                style =
                    MaterialTheme.typography
                        .titleLarge
            )
        }
    }
}

@Composable
private fun FuelEntryCard(
    entry: FuelEntry,
    currencyFormatter: NumberFormat
) {
    Card(
        modifier =
            Modifier.fillMaxWidth()
    ) {
        Column(
            modifier =
                Modifier.padding(14.dp)
        ) {
            Text(
                text =
                    entry.date.take(10),
                style =
                    MaterialTheme.typography
                        .titleMedium
            )

            Text(
                stringResource(
                    R.string.litres_value,
                    entry.litres
                )
            )

            Text(
                stringResource(
                    R.string.odometer_km_value,
                    entry.odometerKm
                )
            )

            Text(
                currencyFormatter.format(
                    entry.totalCost
                )
            )

            entry.pricePerLitre?.let {
                Text(
                    stringResource(
                        R.string.price_per_litre_value,
                        currencyFormatter.format(it)
                    )
                )
            }

            Text(
                if (entry.isFullTank) {
                    stringResource(
                        R.string.full_tank
                    )
                } else {
                    stringResource(
                        R.string.partial_fill
                    )
                }
            )
        }
    }
}

@Composable
private fun ExpenseCard(
    expense: Expense,
    currencyFormatter: NumberFormat
) {
    Card(
        modifier =
            Modifier.fillMaxWidth()
    ) {
        Column(
            modifier =
                Modifier.padding(14.dp)
        ) {
            Text(
                text =
                    expense.category,
                style =
                    MaterialTheme.typography
                        .titleMedium
            )

            Text(
                expense.date.take(10)
            )

            Text(
                currencyFormatter.format(
                    expense.amount
                )
            )

            if (
                !expense.description
                    .isNullOrBlank()
            ) {
                Text(
                    expense.description
                )
            }
        }
    }
}

@Composable
private fun AddFuelDialog(
    vehicleId: Int,
    onDismiss: () -> Unit,
    onSave: (CreateFuelEntryRequest) -> Unit
) {
    var date by remember {
        mutableStateOf(
            LocalDate.now().toString()
        )
    }

    var odometer by remember {
        mutableStateOf("")
    }

    var litres by remember {
        mutableStateOf("")
    }

    var pricePerLitre by remember {
        mutableStateOf("")
    }

    var isFullTank by remember {
        mutableStateOf(true)
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    val invalidFuelMessage =
        stringResource(
            R.string.error_invalid_fuel_entry
        )

    AlertDialog(
        onDismissRequest = onDismiss,

        title = {
            Text(
                stringResource(
                    R.string.add_fuel
                )
            )
        },

        text = {
            Column {
                OutlinedTextField(
                    value = date,
                    onValueChange = {
                        date = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.date_yyyy_mm_dd
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                OutlinedTextField(
                    value = odometer,
                    onValueChange = {
                        odometer = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.odometer_km
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                OutlinedTextField(
                    value = litres,
                    onValueChange = {
                        litres = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.litres
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                OutlinedTextField(
                    value = pricePerLitre,
                    onValueChange = {
                        pricePerLitre = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.price_per_litre
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                Row(
                    verticalAlignment =
                        Alignment.CenterVertically
                ) {
                    Checkbox(
                        checked = isFullTank,
                        onCheckedChange = {
                            isFullTank = it
                        }
                    )

                    Text(
                        stringResource(
                            R.string.full_tank
                        )
                    )
                }

                errorMessage?.let {
                    Spacer(
                        modifier =
                            Modifier.height(6.dp)
                    )

                    Text(
                        text = it,
                        color =
                            MaterialTheme.colorScheme
                                .error
                    )
                }
            }
        },

        confirmButton = {
            TextButton(
                onClick = {
                    val odometerValue =
                        odometer.toIntOrNull()

                    val litresValue =
                        litres.toDoubleOrNull()

                    val priceValue =
                        pricePerLitre
                            .toDoubleOrNull()

                    if (
                        odometerValue == null ||
                        odometerValue < 0 ||
                        litresValue == null ||
                        litresValue <= 0 ||
                        priceValue == null ||
                        priceValue < 0
                    ) {
                        errorMessage =
                            invalidFuelMessage
                    } else {
                        onSave(
                            CreateFuelEntryRequest(
                                vehicleId =
                                    vehicleId,

                                date =
                                    date,

                                odometerKm =
                                    odometerValue,

                                litres =
                                    litresValue,

                                pricePerLitre =
                                    priceValue,

                                totalCost =
                                    litresValue *
                                            priceValue,

                                isFullTank =
                                    isFullTank
                            )
                        )
                    }
                }
            ) {
                Text(
                    stringResource(
                        R.string.save
                    )
                )
            }
        },

        dismissButton = {
            TextButton(
                onClick = onDismiss
            ) {
                Text(
                    stringResource(
                        R.string.cancel
                    )
                )
            }
        }
    )
}

@Composable
private fun AddExpenseDialog(
    vehicleId: Int,
    onDismiss: () -> Unit,
    onSave: (CreateExpenseRequest) -> Unit
) {
    var category by remember {
        mutableStateOf("")
    }

    var amount by remember {
        mutableStateOf("")
    }

    var date by remember {
        mutableStateOf(
            LocalDate.now().toString()
        )
    }

    var description by remember {
        mutableStateOf("")
    }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    val invalidExpenseMessage =
        stringResource(
            R.string.error_invalid_expense
        )

    AlertDialog(
        onDismissRequest = onDismiss,

        title = {
            Text(
                stringResource(
                    R.string.add_expense
                )
            )
        },

        text = {
            Column {
                OutlinedTextField(
                    value = category,
                    onValueChange = {
                        category = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.expense_category
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                OutlinedTextField(
                    value = amount,
                    onValueChange = {
                        amount = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.expense_amount
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                OutlinedTextField(
                    value = date,
                    onValueChange = {
                        date = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.date_yyyy_mm_dd
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                Spacer(
                    modifier =
                        Modifier.height(8.dp)
                )

                OutlinedTextField(
                    value = description,
                    onValueChange = {
                        description = it
                    },
                    label = {
                        Text(
                            stringResource(
                                R.string.description
                            )
                        )
                    },
                    modifier =
                        Modifier.fillMaxWidth()
                )

                errorMessage?.let {
                    Spacer(
                        modifier =
                            Modifier.height(6.dp)
                    )

                    Text(
                        text = it,
                        color =
                            MaterialTheme.colorScheme
                                .error
                    )
                }
            }
        },

        confirmButton = {
            TextButton(
                onClick = {
                    val amountValue =
                        amount.toDoubleOrNull()

                    if (
                        category.isBlank() ||
                        amountValue == null ||
                        amountValue < 0
                    ) {
                        errorMessage =
                            invalidExpenseMessage
                    } else {
                        onSave(
                            CreateExpenseRequest(
                                vehicleId =
                                    vehicleId,

                                category =
                                    category
                                        .trim()
                                        .uppercase(),

                                amount =
                                    amountValue,

                                date =
                                    date,

                                description =
                                    description
                                        .trim()
                                        .ifBlank {
                                            null
                                        }
                            )
                        )
                    }
                }
            ) {
                Text(
                    stringResource(
                        R.string.save
                    )
                )
            }
        },

        dismissButton = {
            TextButton(
                onClick = onDismiss
            ) {
                Text(
                    stringResource(
                        R.string.cancel
                    )
                )
            }
        }
    )
}