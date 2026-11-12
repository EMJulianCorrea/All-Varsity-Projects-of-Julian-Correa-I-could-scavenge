package com.drivemate.sa.ui.vehicles

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalConfiguration
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.Vehicle
import com.drivemate.sa.util.RenewalStatus
import com.drivemate.sa.util.VehicleRenewalUtils
import java.text.NumberFormat
import java.text.SimpleDateFormat
import java.util.Locale

@Composable
fun VehicleProfileScreen(
    vehicles: List<Vehicle>,
    isLoading: Boolean,
    errorMessage: String?,
    statusMessage: String?,
    onAddVehicle: () -> Unit,
    onRefresh: () -> Unit,
    onSignOut: () -> Unit,
    onOpenMaintenance: () -> Unit,
    onOpenRunningCosts: () -> Unit,
    onOpenSettings: () -> Unit,
) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(24.dp),
        verticalArrangement =
            Arrangement.spacedBy(16.dp)
    ) {
        Text(
            text = stringResource(
                R.string.my_vehicles
            ),
            style =
                MaterialTheme.typography.headlineMedium,
            fontWeight =
                FontWeight.Bold
        )

        Text(
            text = stringResource(
                R.string.vehicle_profiles_subtitle
            ),
            style =
                MaterialTheme.typography.bodyMedium
        )

        statusMessage?.let { message ->
            Text(
                text = message,
                color =
                    MaterialTheme.colorScheme.primary,
                fontWeight =
                    FontWeight.Medium
            )
        }

        when {
            isLoading -> {
                Text(
                    stringResource(
                        R.string.loading_vehicles
                    )
                )
            }

            errorMessage != null -> {
                Text(
                    text = errorMessage,
                    color =
                        MaterialTheme.colorScheme.error
                )

                Button(
                    modifier =
                        Modifier.fillMaxWidth(),
                    onClick = onRefresh
                ) {
                    Text(
                        stringResource(
                            R.string.try_again
                        )
                    )
                }
            }

            vehicles.isEmpty() -> {
                Text(
                    text = stringResource(
                        R.string.no_vehicles
                    )
                )
            }

            else -> {
                vehicles.forEach { vehicle ->
                    VehicleCard(vehicle)
                }
            }
        }

        Spacer(
            modifier = Modifier.height(8.dp)
        )

        Button(
            modifier =
                Modifier.fillMaxWidth(),
            onClick = onAddVehicle
        ) {
            Text(
                stringResource(
                    R.string.add_vehicle
                )
            )
        }

        Button(
            modifier =
                Modifier.fillMaxWidth(),
            onClick = onOpenMaintenance
        ) {
            Text(
                stringResource(
                    R.string.vehicle_health
                )
            )
        }

        Spacer(
            modifier = Modifier.height(8.dp)
        )

        Button(
            onClick = onOpenRunningCosts,
            modifier =
                Modifier.fillMaxWidth()
        ) {
            Text(
                stringResource(
                    R.string.running_costs
                )
            )
        }

        Spacer(
            modifier = Modifier.height(8.dp)
        )

        Button(
            onClick = onOpenSettings,
            modifier =
                Modifier.fillMaxWidth()
        ) {
            Text(
                stringResource(
                    R.string.settings
                )
            )
        }

        OutlinedButton(
            modifier =
                Modifier.fillMaxWidth(),
            onClick = onSignOut
        ) {
            Text(
                stringResource(
                    R.string.sign_out
                )
            )
        }

        Spacer(
            modifier = Modifier.height(24.dp)
        )
    }
}

@Composable
private fun VehicleCard(
    vehicle: Vehicle
) {
    val configuration =
        LocalConfiguration.current

    val locale =
        configuration.locales[0]

    val numberFormatter =
        NumberFormat.getIntegerInstance(
            locale
        )

    val daysRemaining =
        VehicleRenewalUtils.daysUntilRenewal(
            vehicle.licenceRenewalDate
        )

    val formattedRenewalDate =
        formatRenewalDate(
            vehicle.licenceRenewalDate,
            locale
        )

    Card(
        modifier =
            Modifier.fillMaxWidth(),
        elevation =
            CardDefaults.cardElevation(
                defaultElevation = 4.dp
            )
    ) {
        Column(
            modifier =
                Modifier.padding(20.dp),
            verticalArrangement =
                Arrangement.spacedBy(8.dp)
        ) {
            Text(
                text =
                    vehicle.nickname.ifBlank {
                        "${vehicle.make} ${vehicle.model}"
                    },
                style =
                    MaterialTheme.typography.titleLarge,
                fontWeight =
                    FontWeight.Bold
            )

            Text(
                text =
                    "${vehicle.make} " +
                            "${vehicle.model} " +
                            "\u2022 ${vehicle.year}",
                style =
                    MaterialTheme.typography.bodyLarge
            )

            Text(
                text =
                    vehicle.registrationNumber,
                style =
                    MaterialTheme.typography.titleMedium
            )

            Spacer(
                modifier = Modifier.height(4.dp)
            )

            Text(
                text = stringResource(
                    R.string.odometer_value,
                    numberFormatter.format(
                        vehicle.odometerKm
                    )
                )
            )

            Text(
                text = stringResource(
                    R.string.fuel_label,
                    vehicle.fuelType
                )
            )

            Spacer(
                modifier = Modifier.height(8.dp)
            )

            Text(
                text = stringResource(
                    R.string.licence_renewal
                ),
                fontWeight =
                    FontWeight.Bold
            )

            Text(
                text =
                    formattedRenewalDate
            )

            if (daysRemaining == null) {
                Text(
                    text = stringResource(
                        R.string.renewal_unavailable
                    ),
                    color =
                        MaterialTheme.colorScheme.error
                )
            } else {
                when (
                    VehicleRenewalUtils
                        .renewalStatus(
                            daysRemaining
                        )
                ) {
                    RenewalStatus.OVERDUE -> {
                        Text(
                            text = stringResource(
                                R.string.renewal_overdue,
                                -daysRemaining
                            ),
                            color =
                                MaterialTheme.colorScheme
                                    .error,
                            fontWeight =
                                FontWeight.Bold
                        )
                    }

                    RenewalStatus.DUE_TODAY -> {
                        Text(
                            text = stringResource(
                                R.string.renewal_due_today
                            ),
                            color =
                                MaterialTheme.colorScheme
                                    .error,
                            fontWeight =
                                FontWeight.Bold
                        )
                    }

                    RenewalStatus.DUE_SOON -> {
                        Text(
                            text = stringResource(
                                R.string.renewal_due_soon,
                                daysRemaining
                            ),
                            color =
                                MaterialTheme.colorScheme
                                    .error,
                            fontWeight =
                                FontWeight.Bold
                        )
                    }

                    RenewalStatus.OK -> {
                        Text(
                            text = stringResource(
                                R.string.renewal_days_remaining,
                                daysRemaining
                            )
                        )
                    }
                }
            }
        }
    }
}

private fun formatRenewalDate(
    dateValue: String,
    locale: Locale
): String {
    return try {
        val inputFormatter =
            SimpleDateFormat(
                "yyyy-MM-dd",
                Locale.US
            )

        val outputFormatter =
            SimpleDateFormat(
                "d MMM yyyy",
                locale
            )

        val date =
            inputFormatter.parse(
                dateValue.take(10)
            )

        if (date != null) {
            outputFormatter.format(date)
        } else {
            dateValue.take(10)
        }

    } catch (e: Exception) {
        dateValue.take(10)
    }
}