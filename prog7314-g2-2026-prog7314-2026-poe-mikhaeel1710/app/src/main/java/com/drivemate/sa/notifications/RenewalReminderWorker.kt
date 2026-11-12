package com.drivemate.sa.notifications

import android.content.Context
import android.util.Log
import androidx.work.CoroutineWorker
import androidx.work.ExistingPeriodicWorkPolicy
import androidx.work.PeriodicWorkRequestBuilder
import androidx.work.WorkManager
import androidx.work.WorkerParameters
import com.drivemate.sa.R
import com.drivemate.sa.network.RetrofitClient
import com.drivemate.sa.util.RenewalStatus
import com.drivemate.sa.util.VehicleRenewalUtils
import com.google.firebase.auth.FirebaseAuth
import java.util.concurrent.TimeUnit

/**
 * Runs about once a day and posts a reminder for each of the signed-in user's
 * vehicles whose licence renewal is due soon, due today or overdue.
 */
class RenewalReminderWorker(
    context: Context,
    params: WorkerParameters
) : CoroutineWorker(context, params) {

    override suspend fun doWork(): Result {
        val context = applicationContext

        if (!NotificationPreferences.isEnabled(context)) return Result.success()

        val uid = FirebaseAuth.getInstance().currentUser?.uid
            ?: return Result.success()

        return try {
            val vehicles = RetrofitClient.api.getVehiclesForUser(uid)

            vehicles.forEach { vehicle ->
                val days = VehicleRenewalUtils
                    .daysUntilRenewal(vehicle.licenceRenewalDate)
                    ?: return@forEach

                val message = when (VehicleRenewalUtils.renewalStatus(days)) {
                    RenewalStatus.OVERDUE ->
                        context.getString(R.string.renewal_overdue, -days)
                    RenewalStatus.DUE_TODAY ->
                        context.getString(R.string.renewal_due_today)
                    RenewalStatus.DUE_SOON ->
                        context.getString(R.string.renewal_due_soon, days)
                    RenewalStatus.OK -> return@forEach
                }

                SecureNotificationHelper.showReminder(
                    context = context,
                    notificationId = vehicle.id,
                    title = vehicle.nickname.ifBlank {
                        "${vehicle.make} ${vehicle.model}"
                    },
                    message = message
                )
            }

            Result.success()
        } catch (e: Exception) {
            Log.e(TAG, "Renewal reminder check failed", e)
            Result.retry()
        }
    }

    companion object {
        private const val TAG = "DriveMateNotify"
        private const val WORK_NAME = "drivemate_renewal_reminders"

        fun schedule(context: Context) {
            val request = PeriodicWorkRequestBuilder<RenewalReminderWorker>(
                1, TimeUnit.DAYS
            ).build()

            WorkManager.getInstance(context).enqueueUniquePeriodicWork(
                WORK_NAME,
                ExistingPeriodicWorkPolicy.UPDATE,
                request
            )
        }

        fun cancel(context: Context) {
            WorkManager.getInstance(context).cancelUniqueWork(WORK_NAME)
        }
    }
}
