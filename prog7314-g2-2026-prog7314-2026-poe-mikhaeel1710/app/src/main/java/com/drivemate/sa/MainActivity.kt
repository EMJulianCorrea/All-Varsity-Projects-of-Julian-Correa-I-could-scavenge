package com.drivemate.sa

import androidx.compose.ui.res.stringResource
import com.drivemate.sa.locale.LanguageManager
import androidx.compose.runtime.saveable.rememberSaveable
import com.drivemate.sa.ui.screens.SettingScreen
import com.drivemate.sa.ui.screens.RunningCostsScreen
import com.drivemate.sa.viewmodels.RunningCostsViewModel
import androidx.compose.runtime.collectAsState
import androidx.lifecycle.viewmodel.compose.viewModel
import com.drivemate.sa.ui.screens.MaintenanceScreen
import com.drivemate.sa.viewmodels.MaintenanceViewModel
import android.os.Bundle
import android.util.Log
import androidx.fragment.app.FragmentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.credentials.CredentialManager
import androidx.credentials.CustomCredential
import androidx.credentials.GetCredentialRequest
import androidx.lifecycle.lifecycleScope
import com.drivemate.sa.network.CreateCloudTestRequest
import com.drivemate.sa.network.RetrofitClient
import com.drivemate.sa.network.Vehicle
import com.drivemate.sa.ui.theme.DriveMateTheme
import com.drivemate.sa.ui.vehicles.AddVehicleScreen
import com.drivemate.sa.ui.vehicles.VehicleProfileScreen
import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential.Companion.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
import com.google.firebase.FirebaseNetworkException
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.auth.FirebaseAuthInvalidCredentialsException
import com.google.firebase.auth.FirebaseAuthInvalidUserException
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import com.drivemate.sa.security.LoginAttemptTracker
import com.drivemate.sa.security.SharedPrefsAttemptStore
import kotlin.math.ceil
import com.google.firebase.auth.GoogleAuthProvider
import kotlinx.coroutines.launch

class MainActivity : FragmentActivity() {
    companion object {
        private const val TAG_APP = "DriveMateApp"
        private const val TAG_AUTH = "DriveMateAuth"
        private const val TAG_API = "DriveMateApi"
        private const val TAG_CLOUD = "DriveMateCloud"
        private const val TAG_VEHICLE = "DriveMateVehicle"
    }

    private lateinit var auth: FirebaseAuth
    private lateinit var credentialManager: CredentialManager

    private val loginAttemptTracker by lazy {
        LoginAttemptTracker(SharedPrefsAttemptStore(this))
    }

    private var isLoading by mutableStateOf(false)
    private var errorMessage by mutableStateOf<String?>(null)
    private var refreshUi by mutableStateOf(0)

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        LanguageManager.applyStoredLanguage(this)
        Log.i(
            TAG_APP,
            "DriveMate application started"
        )

        auth = FirebaseAuth.getInstance()
        credentialManager = CredentialManager.create(this)

        if (auth.currentUser != null) {
            Log.i(
                TAG_AUTH,
                "Existing Firebase session detected - auto login"
            )
        } else {
            Log.d(
                TAG_AUTH,
                "No existing Firebase session"
            )
        }

        setContent {
            DriveMateTheme {

                val ignored = refreshUi
                val currentUser = auth.currentUser
                val checkingApiText =
                    stringResource(R.string.checking_api)

                val cloudNotTestedText =
                    stringResource(R.string.cloud_not_tested)

                val apiConnectedText =
                    stringResource(R.string.api_connected)

                val apiFailedText =
                    stringResource(R.string.api_connection_failed)

                val cloudConnectedText =
                    stringResource(R.string.cloud_database_connected)

                val cloudNoRecordsText =
                    stringResource(R.string.cloud_record_not_returned)

                val cloudFailedText =
                    stringResource(R.string.cloud_database_test_failed)

                var apiStatus by remember {
                    mutableStateOf(checkingApiText)
                }

                var cloudStatus by remember {
                    mutableStateOf(cloudNotTestedText)
                }

                var cloudLoading by remember {
                    mutableStateOf(false)
                }

                var showAddVehicle by remember {
                    mutableStateOf(false)
                }
                var showMaintenance by remember {
                    mutableStateOf(false)
                }
                var showRunningCosts by remember {
                    mutableStateOf(false)
                }
                var showSettings by rememberSaveable {
                    mutableStateOf(false)
                }
                var emailInput by rememberSaveable {
                    mutableStateOf("")
                }
                // Deliberately not rememberSaveable: never write the
                // password into saved instance state.
                var passwordInput by remember {
                    mutableStateOf("")
                }
                var vehicleSaveStatus by remember {
                    mutableStateOf<String?>(null)
                }

                var vehicleSaving by remember {
                    mutableStateOf(false)
                }

                var vehicles by remember {
                    mutableStateOf<List<Vehicle>>(
                        emptyList()
                    )
                }

                var vehiclesLoading by remember {
                    mutableStateOf(false)
                }

                var vehiclesError by remember {
                    mutableStateOf<String?>(null)
                }

                var vehicleRefreshKey by remember {
                    mutableStateOf(0)
                }

                LaunchedEffect(Unit) {

                    Log.d(
                        TAG_API,
                        "Checking DriveMate API health"
                    )

                    apiStatus = try {
                        val response =
                            RetrofitClient.api.getHealth()

                        Log.i(
                            TAG_API,
                            "API health check successful: " +
                                    "${response.service} ${response.status}"
                        )

                        apiConnectedText
                    } catch (e: Exception) {

                        Log.e(
                            TAG_API,
                            "API health check failed",
                            e
                        )

                        apiFailedText}
                }

                LaunchedEffect(
                    currentUser?.uid,
                    vehicleRefreshKey
                ) {
                    if (currentUser == null) {

                        Log.d(
                            TAG_VEHICLE,
                            "No signed-in user. Clearing vehicle list"
                        )

                        vehicles = emptyList()
                        vehiclesLoading = false
                        vehiclesError = null

                    } else {

                        Log.d(
                            TAG_VEHICLE,
                            "Loading vehicles for signed-in user"
                        )

                        vehiclesLoading = true
                        vehiclesError = null

                        try {
                            vehicles =
                                RetrofitClient.api
                                    .getVehiclesForUser(
                                        currentUser.uid
                                    )

                            Log.i(
                                TAG_VEHICLE,
                                "Vehicles loaded successfully. Count: " +
                                        vehicles.size
                            )

                        } catch (e: Exception) {

                            Log.e(
                                TAG_VEHICLE,
                                "Failed to load vehicles",
                                e
                            )

                            vehiclesError =
                                getString(
                                    R.string.failed_load_vehicles
                                )

                        } finally {
                            vehiclesLoading = false

                            Log.d(
                                TAG_VEHICLE,
                                "Vehicle loading operation finished"
                            )
                        }
                    }
                }

                when {
                    currentUser != null &&
                            showSettings -> {

                        SettingScreen(
                            currentUser = currentUser,

                            onBackClick = {
                                showSettings = false
                            },

                            onViewFullProfile = { },

                            onMyBookingsClick = { },

                            onMyMessagesClick = { },

                            onHelpCenterClick = { },

                            onAppFeedbackClick = { },

                            onLanguageClick = {
                                Log.d(
                                    TAG_APP,
                                    "Language settings opened"
                                )
                            },

                            onNotificationClick = { }
                        )
                    }
                    currentUser != null &&
                            showRunningCosts -> {

                        val runningCostsViewModel:
                                RunningCostsViewModel = viewModel()

                        val selectedVehicleId by
                        runningCostsViewModel.selectedVehicleId.collectAsState()

                        val fuelEntries by
                        runningCostsViewModel.fuelEntries.collectAsState()

                        val expenses by
                        runningCostsViewModel.expenses.collectAsState()

                        LaunchedEffect(
                            vehicles,
                            selectedVehicleId
                        ) {
                            if (
                                selectedVehicleId == null &&
                                vehicles.isNotEmpty()
                            ) {
                                runningCostsViewModel
                                    .selectVehicle(
                                        vehicles.first().id
                                    )
                            }
                        }

                        RunningCostsScreen(
                            vehicles = vehicles,

                            selectedVehicleId =
                                selectedVehicleId,

                            fuelEntries =
                                fuelEntries,

                            expenses =
                                expenses,

                            onSelectVehicle = {
                                runningCostsViewModel
                                    .selectVehicle(it)
                            },

                            onAddFuelEntry = {
                                runningCostsViewModel
                                    .addFuelEntry(it)
                            },

                            onAddExpense = {
                                runningCostsViewModel
                                    .addExpense(it)
                            },

                            onBack = {
                                showRunningCosts = false
                            }
                        )
                    }
                    currentUser != null &&
                            showMaintenance -> {

                        val maintenanceViewModel:
                                MaintenanceViewModel = viewModel()

                        val maintenanceRecords by
                        maintenanceViewModel.records.collectAsState()

                        MaintenanceScreen(
                            records = maintenanceRecords,

                            onAddRecord = {
                                maintenanceViewModel.addRecord(it)
                            },

                            onMarkCompleted = {
                                maintenanceViewModel.markCompleted(it)
                            },

                            onDeleteRecord = {
                                maintenanceViewModel.deleteRecord(it)
                            },

                            onBack = {
                                showMaintenance = false
                            }
                        )
                    }
                    currentUser != null &&
                            showAddVehicle -> {

                        AddVehicleScreen(
                            firebaseUserId =
                                currentUser.uid,

                            onSaveVehicle = { request ->

                                lifecycleScope.launch {

                                    Log.i(
                                        TAG_VEHICLE,
                                        "Vehicle save requested"
                                    )

                                    vehicleSaving = true
                                    vehicleSaveStatus = null

                                    try {
                                        val vehicle =
                                            RetrofitClient.api
                                                .createVehicle(
                                                    request
                                                )

                                        Log.i(
                                            TAG_VEHICLE,
                                            "Vehicle saved successfully: " +
                                                    "${vehicle.make} " +
                                                    vehicle.model
                                        )

                                        vehicleSaveStatus =
                                            getString(
                                                R.string.vehicle_saved_message,
                                                vehicle.make,
                                                vehicle.model
                                            )

                                        showAddVehicle = false
                                        vehicleRefreshKey++

                                    } catch (e: Exception) {

                                        Log.e(
                                            TAG_VEHICLE,
                                            "Failed to save vehicle",
                                            e
                                        )

                                        vehicleSaveStatus =
                                            getString(
                                                R.string.failed_save_vehicle
                                            )

                                    } finally {
                                        vehicleSaving = false

                                        Log.d(
                                            TAG_VEHICLE,
                                            "Vehicle save operation finished"
                                        )
                                    }
                                }
                            },

                            onBack = {
                                Log.d(
                                    TAG_VEHICLE,
                                    "User exited Add Vehicle screen"
                                )

                                vehicleSaveStatus = null
                                showAddVehicle = false
                            },

                            saveStatus =
                                vehicleSaveStatus,

                            isSaving =
                                vehicleSaving
                        )
                    }

                    currentUser != null -> {

                        VehicleProfileScreen(
                            onOpenSettings = {

                                Log.d(
                                    TAG_APP,
                                    "Opening Settings screen"
                                )

                                showAddVehicle = false
                                showMaintenance = false
                                showRunningCosts = false
                                showSettings = true
                            },
                            vehicles = vehicles,

                            isLoading =
                                vehiclesLoading,

                            errorMessage =
                                vehiclesError,

                            statusMessage =
                                vehicleSaveStatus,

                            onOpenMaintenance = {

                                Log.d(
                                    TAG_VEHICLE,
                                    "Opening Vehicle Health screen"
                                )

                                showAddVehicle = false
                                showRunningCosts = false
                                showMaintenance = true
                                showSettings = false
                            },
                            onOpenRunningCosts = {

                                Log.d(
                                    TAG_VEHICLE,
                                    "Opening Running Costs screen"
                                )

                                showAddVehicle = false
                                showMaintenance = false
                                showRunningCosts = true
                                showSettings = false
                            },
                            onAddVehicle = {

                                Log.d(
                                    TAG_VEHICLE,
                                    "Opening Add Vehicle screen"
                                )

                                vehicleSaveStatus = null
                                showMaintenance = false
                                showAddVehicle = true
                                showRunningCosts = false
                                showSettings = false
                            },


                            onRefresh = {

                                Log.i(
                                    TAG_VEHICLE,
                                    "Manual vehicle refresh requested"
                                )

                                vehicleRefreshKey++
                            },
                            onSignOut = {

                                Log.i(
                                    TAG_AUTH,
                                    "User signing out"
                                )

                                auth.signOut()

                                errorMessage = null
                                vehicleSaveStatus = null
                                vehicles = emptyList()
                                showAddVehicle = false
                                showMaintenance = false
                                showRunningCosts = false
                                showSettings = false

                                refreshUi++

                                Log.i(
                                    TAG_AUTH,
                                    "User signed out successfully"
                                )
                            }
                        )
                    }

                    else -> {

                        Column(
                            modifier = Modifier
                                .fillMaxSize()
                                .verticalScroll(
                                    rememberScrollState()
                                )
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
                                    MaterialTheme.typography
                                        .headlineLarge,

                                fontWeight =
                                    FontWeight.Bold
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(8.dp)
                            )

                            Text(
                                text = stringResource(
                                    R.string.your_vehicle_companion
                                ),

                                style =
                                    MaterialTheme.typography
                                        .bodyLarge
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(12.dp)
                            )

                            Text(
                                text = apiStatus,

                                style =
                                    MaterialTheme.typography
                                        .bodyMedium
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(16.dp)
                            )

                            Button(
                                modifier =
                                    Modifier.fillMaxWidth(),

                                enabled =
                                    !cloudLoading,

                                onClick = {
                                    lifecycleScope.launch {

                                        Log.i(
                                            TAG_CLOUD,
                                            "Cloud database test started"
                                        )

                                        cloudLoading = true

                                        cloudStatus = try {

                                            RetrofitClient.api
                                                .createCloudTestRecord(
                                                    CreateCloudTestRequest(
                                                        message =
                                                            "Hello from DriveMate Android"
                                                    )
                                                )

                                            Log.d(
                                                TAG_CLOUD,
                                                "Cloud test record created"
                                            )

                                            val records =
                                                RetrofitClient.api
                                                    .getCloudTestRecords()

                                            Log.d(
                                                TAG_CLOUD,
                                                "Cloud test records retrieved. Count: " +
                                                        records.size
                                            )

                                            val latestRecord =
                                                records.firstOrNull()

                                            if (
                                                latestRecord != null
                                            ) {

                                                Log.i(
                                                    TAG_CLOUD,
                                                    "Cloud database test successful"
                                                )

                                                cloudConnectedText

                                            } else {

                                                Log.w(
                                                    TAG_CLOUD,
                                                    "Cloud record created but no records returned"
                                                )

                                                cloudNoRecordsText                                            }

                                        } catch (
                                            e: Exception
                                        ) {

                                            Log.e(
                                                TAG_CLOUD,
                                                "Cloud database test failed",
                                                e
                                            )

                                            cloudFailedText

                                        } finally {
                                            cloudLoading = false

                                            Log.d(
                                                TAG_CLOUD,
                                                "Cloud database test finished"
                                            )
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
                                modifier =
                                    Modifier.height(12.dp)
                            )

                            Text(
                                text = cloudStatus,

                                style =
                                    MaterialTheme.typography
                                        .bodyMedium
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(40.dp)
                            )
                            Text(
                                text = stringResource(
                                    R.string.sign_in_to_continue
                                ),

                                style =
                                    MaterialTheme.typography
                                        .titleMedium
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(20.dp)
                            )

                            Button(
                                modifier =
                                    Modifier.fillMaxWidth(),

                                enabled =
                                    !isLoading,

                                onClick = {

                                    Log.d(
                                        TAG_AUTH,
                                        "Google sign-in button selected"
                                    )

                                    signInWithGoogle()
                                }
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

                            Spacer(
                                modifier =
                                    Modifier.height(24.dp)
                            )

                            OutlinedTextField(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                value = emailInput,
                                onValueChange = {
                                    emailInput = it
                                },
                                label = {
                                    Text(
                                        stringResource(
                                            R.string.email_label
                                        )
                                    )
                                },
                                singleLine = true,
                                keyboardOptions =
                                    KeyboardOptions(
                                        keyboardType =
                                            KeyboardType.Email
                                    )
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(8.dp)
                            )

                            OutlinedTextField(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                value = passwordInput,
                                onValueChange = {
                                    passwordInput = it
                                },
                                label = {
                                    Text(
                                        stringResource(
                                            R.string.password_label
                                        )
                                    )
                                },
                                singleLine = true,
                                visualTransformation =
                                    PasswordVisualTransformation(),
                                keyboardOptions =
                                    KeyboardOptions(
                                        keyboardType =
                                            KeyboardType.Password
                                    )
                            )

                            Spacer(
                                modifier =
                                    Modifier.height(12.dp)
                            )

                            Button(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                enabled = !isLoading,
                                onClick = {
                                    signInWithEmail(
                                        emailInput,
                                        passwordInput
                                    )
                                }
                            ) {
                                Text(
                                    stringResource(
                                        R.string.sign_in_with_email
                                    )
                                )
                            }

                            Spacer(
                                modifier =
                                    Modifier.height(8.dp)
                            )

                            OutlinedButton(
                                modifier =
                                    Modifier.fillMaxWidth(),
                                enabled = !isLoading,
                                onClick = {
                                    createAccountWithEmail(
                                        emailInput,
                                        passwordInput
                                    )
                                }
                            ) {
                                Text(
                                    stringResource(
                                        R.string.create_account
                                    )
                                )
                            }

                            errorMessage?.let {
                                    message ->

                                Spacer(
                                    modifier =
                                        Modifier.height(16.dp)
                                )

                                Text(
                                    text = message,

                                    color =
                                        MaterialTheme
                                            .colorScheme
                                            .error
                                )
                            }
                        }
                    }
                }
            }
        }
    }

    private fun lockedOutMessage(lockedUntilMillis: Long): String {
        val remainingMillis =
            lockedUntilMillis - System.currentTimeMillis()

        val minutes =
            ceil(remainingMillis / 60_000.0)
                .toInt()
                .coerceAtLeast(1)

        return getString(R.string.login_locked_out, minutes)
    }

    /**
     * Email/password sign-in with a limit of
     * [LoginAttemptTracker.MAX_ATTEMPTS] failed attempts before the
     * account is locked out on this device.
     */
    private fun signInWithEmail(
        email: String,
        password: String
    ) {
        val cleanEmail = email.trim()

        if (cleanEmail.isEmpty() || password.isEmpty()) {
            errorMessage =
                getString(R.string.login_fields_required)
            return
        }

        val state = loginAttemptTracker.check(cleanEmail)

        if (state is LoginAttemptTracker.State.LockedOut) {
            Log.w(
                TAG_AUTH,
                "Password sign-in blocked: too many failed attempts"
            )
            errorMessage =
                lockedOutMessage(state.lockedUntilMillis)
            return
        }

        isLoading = true
        errorMessage = null

        auth.signInWithEmailAndPassword(cleanEmail, password)
            .addOnCompleteListener(this) { task ->

                isLoading = false

                if (task.isSuccessful) {

                    Log.i(
                        TAG_AUTH,
                        "Email sign-in successful"
                    )

                    loginAttemptTracker.recordSuccess(cleanEmail)
                    errorMessage = null
                    refreshUi++

                } else {

                    val exception = task.exception

                    Log.w(
                        TAG_AUTH,
                        "Email sign-in failed",
                        exception
                    )

                    errorMessage = when (exception) {

                        is FirebaseAuthInvalidCredentialsException,
                        is FirebaseAuthInvalidUserException -> {
                            when (
                                val result =
                                    loginAttemptTracker
                                        .recordFailure(cleanEmail)
                            ) {
                                is LoginAttemptTracker.State.LockedOut ->
                                    lockedOutMessage(
                                        result.lockedUntilMillis
                                    )

                                is LoginAttemptTracker.State.Allowed ->
                                    getString(
                                        R.string.login_invalid_credentials,
                                        result.attemptsRemaining
                                    )
                            }
                        }

                        is FirebaseNetworkException ->
                            getString(R.string.login_network_error)

                        else ->
                            getString(
                                R.string.firebase_authentication_failed
                            )
                    }
                }
            }
    }

    private fun createAccountWithEmail(
        email: String,
        password: String
    ) {
        val cleanEmail = email.trim()

        if (cleanEmail.isEmpty() || password.isEmpty()) {
            errorMessage =
                getString(R.string.login_fields_required)
            return
        }

        if (password.length < 6) {
            errorMessage =
                getString(R.string.login_password_too_short)
            return
        }

        isLoading = true
        errorMessage = null

        auth.createUserWithEmailAndPassword(cleanEmail, password)
            .addOnCompleteListener(this) { task ->

                isLoading = false

                if (task.isSuccessful) {

                    Log.i(
                        TAG_AUTH,
                        "Email account created"
                    )

                    loginAttemptTracker.recordSuccess(cleanEmail)
                    errorMessage = null
                    refreshUi++

                } else {

                    Log.e(
                        TAG_AUTH,
                        "Email account creation failed",
                        task.exception
                    )

                    errorMessage =
                        if (task.exception is FirebaseNetworkException) {
                            getString(R.string.login_network_error)
                        } else {
                            getString(R.string.account_creation_failed)
                        }
                }
            }
    }

    private fun signInWithGoogle() {

        Log.i(
            TAG_AUTH,
            "Starting Google credential request"
        )

        isLoading = true
        errorMessage = null

        val signInWithGoogleOption =
            GetSignInWithGoogleOption.Builder(
                serverClientId =
                    getString(
                        R.string.default_web_client_id
                    )
            ).build()

        val request =
            GetCredentialRequest.Builder()
                .addCredentialOption(
                    signInWithGoogleOption
                )
                .build()

        lifecycleScope.launch {

            try {
                val result =
                    credentialManager.getCredential(
                        context =
                            this@MainActivity,

                        request = request
                    )

                Log.d(
                    TAG_AUTH,
                    "Credential Manager returned a credential"
                )

                val credential =
                    result.credential

                if (
                    credential is CustomCredential &&
                    credential.type ==
                    TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
                ) {

                    Log.d(
                        TAG_AUTH,
                        "Google ID token credential received"
                    )

                    val googleCredential =
                        GoogleIdTokenCredential
                            .createFrom(
                                credential.data
                            )

                    firebaseAuthWithGoogle(
                        googleCredential.idToken
                    )

                } else {

                    Log.w(
                        TAG_AUTH,
                        "Unsupported credential type returned"
                    )

                    isLoading = false

                    errorMessage =
                        getString(
                            R.string.google_sign_in_unsupported
                        )
                }

            } catch (e: Exception) {

                Log.e(
                    TAG_AUTH,
                    "Google sign-in failed",
                    e
                )

                isLoading = false

                errorMessage =
                    getString(
                        R.string.google_sign_in_failed
                    )
            }
        }
    }

    private fun firebaseAuthWithGoogle(
        idToken: String
    ) {

        Log.d(
            TAG_AUTH,
            "Authenticating Google credential with Firebase"
        )

        val firebaseCredential =
            GoogleAuthProvider.getCredential(
                idToken,
                null
            )

        auth.signInWithCredential(
            firebaseCredential
        ).addOnCompleteListener(this) {
                task ->

            isLoading = false

            if (task.isSuccessful) {

                Log.i(
                    TAG_AUTH,
                    "Firebase Google authentication successful"
                )

                errorMessage = null
                refreshUi++

            } else {

                Log.e(
                    TAG_AUTH,
                    "Firebase authentication failed",
                    task.exception
                )

                errorMessage =
                    getString(
                        R.string.firebase_authentication_failed
                    )
            }
        }
    }
}