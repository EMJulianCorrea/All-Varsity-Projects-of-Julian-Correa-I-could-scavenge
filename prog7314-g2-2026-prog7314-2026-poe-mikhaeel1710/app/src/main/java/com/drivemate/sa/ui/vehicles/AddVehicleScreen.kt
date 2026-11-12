package com.drivemate.sa.ui.vehicles

import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.CreateVehicleRequest

@Composable
fun AddVehicleScreen(
    firebaseUserId: String,
    onSaveVehicle: (CreateVehicleRequest) -> Unit,
    onBack: () -> Unit,
    saveStatus: String? = null,
    isSaving: Boolean = false
) {
    var nickname by remember { mutableStateOf("") }
    var make by remember { mutableStateOf("") }
    var model by remember { mutableStateOf("") }
    var year by remember { mutableStateOf("") }
    var registrationNumber by remember { mutableStateOf("") }
    var fuelType by remember { mutableStateOf("") }
    var odometerKm by remember { mutableStateOf("") }
    var licenceRenewalDate by remember { mutableStateOf("") }

    var errorMessage by remember {
        mutableStateOf<String?>(null)
    }

    // Resolve validation messages while we are still in a composable scope.
    val makeRequired =
        stringResource(R.string.error_vehicle_make_required)

    val modelRequired =
        stringResource(R.string.error_vehicle_model_required)

    val yearInvalid =
        stringResource(R.string.error_vehicle_year_invalid)

    val registrationRequired =
        stringResource(R.string.error_registration_required)

    val fuelRequired =
        stringResource(R.string.error_fuel_type_required)

    val odometerInvalid =
        stringResource(R.string.error_odometer_invalid)

    val renewalRequired =
        stringResource(R.string.error_renewal_date_required)

    val renewalFormatInvalid =
        stringResource(R.string.error_renewal_date_format)

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .imePadding()
            .navigationBarsPadding()
            .padding(24.dp),
        verticalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        Text(
            text = stringResource(R.string.add_vehicle),
            style = MaterialTheme.typography.headlineMedium
        )

        Text(
            text = stringResource(
                R.string.vehicle_profile_subtitle
            ),
            style = MaterialTheme.typography.bodyMedium
        )

        Spacer(
            modifier = Modifier.height(8.dp)
        )

        OutlinedTextField(
            value = nickname,
            onValueChange = { nickname = it },
            label = {
                Text(
                    stringResource(
                        R.string.vehicle_nickname
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.example_daily_car
                    )
                )
            },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = make,
            onValueChange = { make = it },
            label = {
                Text(
                    stringResource(
                        R.string.vehicle_make
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.example_toyota
                    )
                )
            },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = model,
            onValueChange = { model = it },
            label = {
                Text(
                    stringResource(
                        R.string.vehicle_model
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.example_corolla
                    )
                )
            },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = year,
            onValueChange = {
                year = it.filter { character ->
                    character.isDigit()
                }
            },
            label = {
                Text(
                    stringResource(
                        R.string.vehicle_year
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.example_vehicle_year
                    )
                )
            },
            keyboardOptions = KeyboardOptions(
                keyboardType = KeyboardType.Number
            ),
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = registrationNumber,
            onValueChange = {
                registrationNumber =
                    it.uppercase()
            },
            label = {
                Text(
                    stringResource(
                        R.string.registration_number
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.example_registration
                    )
                )
            },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = fuelType,
            onValueChange = {
                fuelType = it
            },
            label = {
                Text(
                    stringResource(
                        R.string.fuel_type
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.fuel_type_options
                    )
                )
            },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = odometerKm,
            onValueChange = {
                odometerKm =
                    it.filter { character ->
                        character.isDigit()
                    }
            },
            label = {
                Text(
                    stringResource(
                        R.string.current_odometer
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.example_odometer
                    )
                )
            },
            keyboardOptions = KeyboardOptions(
                keyboardType =
                    KeyboardType.Number
            ),
            modifier =
                Modifier.fillMaxWidth(),
            singleLine = true
        )

        OutlinedTextField(
            value = licenceRenewalDate,
            onValueChange = {
                licenceRenewalDate = it
            },
            label = {
                Text(
                    stringResource(
                        R.string.licence_renewal_date
                    )
                )
            },
            placeholder = {
                Text(
                    stringResource(
                        R.string.date_format
                    )
                )
            },
            modifier = Modifier.fillMaxWidth(),
            singleLine = true
        )

        errorMessage?.let { message ->
            Text(
                text = message,
                color =
                    MaterialTheme.colorScheme.error
            )
        }

        saveStatus?.let { message ->
            Text(
                text = message,
                color =
                    MaterialTheme.colorScheme.error
            )
        }

        Spacer(
            modifier = Modifier.height(8.dp)
        )

        Button(
            modifier = Modifier.fillMaxWidth(),
            enabled = !isSaving,
            onClick = {
                val yearValue =
                    year.toIntOrNull()

                val odometerValue =
                    odometerKm.toIntOrNull()

                when {
                    make.isBlank() -> {
                        errorMessage =
                            makeRequired
                    }

                    model.isBlank() -> {
                        errorMessage =
                            modelRequired
                    }

                    yearValue == null -> {
                        errorMessage =
                            yearInvalid
                    }

                    registrationNumber.isBlank() -> {
                        errorMessage =
                            registrationRequired
                    }

                    fuelType.isBlank() -> {
                        errorMessage =
                            fuelRequired
                    }

                    odometerValue == null -> {
                        errorMessage =
                            odometerInvalid
                    }

                    licenceRenewalDate.isBlank() -> {
                        errorMessage =
                            renewalRequired
                    }

                    !licenceRenewalDate.matches(
                        Regex(
                            """\d{4}-\d{2}-\d{2}"""
                        )
                    ) -> {
                        errorMessage =
                            renewalFormatInvalid
                    }

                    else -> {
                        errorMessage = null

                        onSaveVehicle(
                            CreateVehicleRequest(
                                firebaseUserId =
                                    firebaseUserId,

                                nickname =
                                    nickname.trim(),

                                make =
                                    make.trim(),

                                model =
                                    model.trim(),

                                year =
                                    yearValue,

                                registrationNumber =
                                    registrationNumber
                                        .trim(),

                                fuelType =
                                    fuelType.trim(),

                                odometerKm =
                                    odometerValue,

                                licenceRenewalDate =
                                    "${licenceRenewalDate}T00:00:00Z"
                            )
                        )
                    }
                }
            }
        ) {
            Text(
                if (isSaving) {
                    stringResource(
                        R.string.saving_vehicle
                    )
                } else {
                    stringResource(
                        R.string.save_vehicle
                    )
                }
            )
        }

        OutlinedButton(
            modifier = Modifier.fillMaxWidth(),
            enabled = !isSaving,
            onClick = onBack
        ) {
            Text(
                stringResource(R.string.back)
            )
        }

        Spacer(
            modifier = Modifier.height(24.dp)
        )
    }
}