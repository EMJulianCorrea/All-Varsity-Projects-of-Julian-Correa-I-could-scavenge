package com.drivemate.sa.ui.screens

import android.util.Log
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.stringResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import com.drivemate.sa.R
import com.drivemate.sa.network.CreateCloudTestRequest
import com.drivemate.sa.network.RetrofitClient
import com.google.firebase.auth.FirebaseUser
import kotlinx.coroutines.launch

@Composable
fun HomeScreen(
    currentUser: FirebaseUser?,
    isLoading: Boolean,
    errorMessage: String?,
    onSignIn: () -> Unit,
    onSignOut: () -> Unit,
    onOpenMaintenance: () -> Unit
) {
    val checkingApiText =
        stringResource(R.string.checking_api)

    val apiConnectedText =
        stringResource(R.string.api_connected)

    val apiConnectionFailedText =
        stringResource(R.string.api_connection_failed)

    val cloudNotTestedText =
        stringResource(R.string.cloud_not_tested)

    val cloudConnectedText =
        stringResource(R.string.cloud_database_connected)

    val cloudNoRecordsText =
        stringResource(R.string.cloud_record_not_returned)

    val cloudFailedText =
        stringResource(R.string.cloud_database_test_failed)

    val cloudTestMessage =
        stringResource(R.string.cloud_test_message)

    var apiStatus by remember(checkingApiText) {
        mutableStateOf(checkingApiText)
    }

    var cloudStatus by remember(cloudNotTestedText) {
        mutableStateOf(cloudNotTestedText)
    }

    var cloudLoading by remember {
        mutableStateOf(false)
    }

    val scope =
        rememberCoroutineScope()

    LaunchedEffect(Unit) {
        apiStatus = try {
            val response =
                RetrofitClient.api.getHealth()

            Log.i(
                "DriveMateApi",
                "API health check successful: " +
                        "${response.service} ${response.status}"
            )

            apiConnectedText
        } catch (e: Exception) {
            Log.e(
                "DriveMateApi",
                "API connection failed",
                e
            )

            apiConnectionFailedText
        }
    }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(32.dp),
        verticalArrangement =
            Arrangement.Center,
        horizontalAlignment =
            Alignment.CenterHorizontally
    ) {
        Text(
            text = stringResource(
                R.string.app_name
            ),
            style =
                MaterialTheme.typography.headlineLarge,
            fontWeight =
                FontWeight.Bold
        )

        Spacer(
            modifier = Modifier.height(8.dp)
        )

        Text(
            text = stringResource(
                R.string.your_vehicle_companion
            ),
            style =
                MaterialTheme.typography.bodyLarge
        )

        Spacer(
            modifier = Modifier.height(12.dp)
        )

        Text(
            text = apiStatus,
            style =
                MaterialTheme.typography.bodyMedium
        )

        Spacer(
            modifier = Modifier.height(16.dp)
        )

        Button(
            modifier =
                Modifier.fillMaxWidth(),
            enabled =
                !cloudLoading,
            onClick = {
                scope.launch {
                    cloudLoading = true

                    cloudStatus = try {
                        RetrofitClient.api
                            .createCloudTestRecord(
                                CreateCloudTestRequest(
                                    message =
                                        cloudTestMessage
                                )
                            )

                        val records =
                            RetrofitClient.api
                                .getCloudTestRecords()

                        val latestRecord =
                            records.firstOrNull()

                        if (latestRecord != null) {
                            cloudConnectedText
                        } else {
                            cloudNoRecordsText
                        }

                    } catch (e: Exception) {
                        Log.e(
                            "DriveMateCloud",
                            "Cloud database test failed",
                            e
                        )

                        cloudFailedText

                    } finally {
                        cloudLoading = false
                    }
                }
            }
        ) {
            Text(
                if (cloudLoading) {
                    stringResource(
                        R.string.testing_cloud_database
                    )
                } else {
                    stringResource(
                        R.string.test_cloud_database
                    )
                }
            )
        }

        Spacer(
            modifier = Modifier.height(12.dp)
        )

        Text(
            text = cloudStatus,
            style =
                MaterialTheme.typography.bodyMedium
        )

        Spacer(
            modifier = Modifier.height(40.dp)
        )

        if (currentUser == null) {
            Text(
                text = stringResource(
                    R.string.sign_in_to_continue
                ),
                style =
                    MaterialTheme.typography.titleMedium
            )

            Spacer(
                modifier = Modifier.height(20.dp)
            )

            Button(
                modifier =
                    Modifier.fillMaxWidth(),
                enabled =
                    !isLoading,
                onClick =
                    onSignIn
            ) {
                if (isLoading) {
                    CircularProgressIndicator()
                } else {
                    Text(
                        stringResource(
                            R.string.sign_in_with_google
                        )
                    )
                }
            }

            errorMessage?.let { message ->
                Spacer(
                    modifier =
                        Modifier.height(16.dp)
                )

                Text(
                    text = message,
                    color =
                        MaterialTheme.colorScheme.error
                )
            }

        } else {
            Text(
                text = stringResource(
                    R.string.welcome
                ),
                style =
                    MaterialTheme.typography.titleLarge
            )

            Spacer(
                modifier = Modifier.height(8.dp)
            )

            Text(
                text =
                    currentUser.displayName
                        ?: stringResource(
                            R.string.drivemate_user
                        )
            )

            currentUser.email?.let { email ->
                Spacer(
                    modifier =
                        Modifier.height(4.dp)
                )

                Text(email)
            }

            Spacer(
                modifier = Modifier.height(24.dp)
            )

            Button(
                modifier =
                    Modifier.fillMaxWidth(),
                onClick =
                    onOpenMaintenance
            ) {
                Text(
                    stringResource(
                        R.string.vehicle_health
                    )
                )
            }

            Spacer(
                modifier = Modifier.height(12.dp)
            )

            OutlinedButton(
                modifier =
                    Modifier.fillMaxWidth(),
                onClick =
                    onSignOut
            ) {
                Text(
                    stringResource(
                        R.string.sign_out
                    )
                )
            }
        }
    }
}