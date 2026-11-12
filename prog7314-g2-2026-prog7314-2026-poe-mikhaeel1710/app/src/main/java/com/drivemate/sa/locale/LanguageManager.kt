package com.drivemate.sa.locale

import android.app.Activity
import android.app.LocaleManager
import android.content.Context
import android.content.res.Configuration
import android.os.Build
import android.os.LocaleList
import java.util.Locale

data class AppLanguage(
    val tag: String,
    val displayName: String
)

object LanguageManager {

    private const val PREFS_NAME = "drivemate_language"
    private const val KEY_LANGUAGE = "selected_language"

    val supportedLanguages = listOf(
        AppLanguage(
            tag = "en-GB",
            displayName = "English (UK)"
        ),
        AppLanguage(
            tag = "af",
            displayName = "Afrikaans"
        ),
        AppLanguage(
            tag = "zu",
            displayName = "isiZulu"
        )
    )

    fun currentLanguageTag(
        context: Context
    ): String {

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            val localeManager =
                context.getSystemService(
                    LocaleManager::class.java
                )

            val locales =
                localeManager.applicationLocales

            if (!locales.isEmpty) {
                return locales[0]
                    ?.toLanguageTag()
                    ?: "en-GB"
            }
        }

        return context
            .getSharedPreferences(
                PREFS_NAME,
                Context.MODE_PRIVATE
            )
            .getString(
                KEY_LANGUAGE,
                "en-GB"
            )
            ?: "en-GB"
    }

    fun currentLanguageDisplayName(
        context: Context
    ): String {

        val tag =
            currentLanguageTag(context)

        return supportedLanguages
            .firstOrNull {
                it.tag == tag
            }
            ?.displayName
            ?: Locale
                .forLanguageTag(tag)
                .getDisplayName(
                    Locale.forLanguageTag(tag)
                )
    }

    fun setLanguage(
        context: Context,
        tag: String
    ) {
        context
            .getSharedPreferences(
                PREFS_NAME,
                Context.MODE_PRIVATE
            )
            .edit()
            .putString(
                KEY_LANGUAGE,
                tag
            )
            .apply()

        if (
            Build.VERSION.SDK_INT >=
            Build.VERSION_CODES.TIRAMISU
        ) {
            val localeManager =
                context.getSystemService(
                    LocaleManager::class.java
                )

            localeManager.applicationLocales =
                LocaleList.forLanguageTags(tag)

        } else {
            applyLegacyLanguage(
                context,
                tag
            )

            (context as? Activity)
                ?.recreate()
        }
    }

    fun applyStoredLanguage(
        context: Context
    ) {
        if (
            Build.VERSION.SDK_INT >=
            Build.VERSION_CODES.TIRAMISU
        ) {
            return
        }

        val tag =
            currentLanguageTag(context)

        applyLegacyLanguage(
            context,
            tag
        )
    }

    private fun applyLegacyLanguage(
        context: Context,
        tag: String
    ) {
        val locale =
            Locale.forLanguageTag(tag)

        Locale.setDefault(locale)

        val configuration =
            Configuration(
                context.resources.configuration
            )

        configuration.setLocale(locale)

        @Suppress("DEPRECATION")
        context.resources.updateConfiguration(
            configuration,
            context.resources.displayMetrics
        )
    }
}