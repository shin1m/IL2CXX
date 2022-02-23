cmake_minimum_required(VERSION 3.16)
project(System.Globalization.Native)
add_library(System.Globalization.Native STATIC
	pal_calendarData.c
	pal_casing.c
	pal_collation.c
	pal_idna.c
	pal_locale.c
	pal_localeNumberData.c
	pal_localeStringData.c
	pal_normalization.c
	pal_timeZoneInfo.c
	pal_icushim_static.c
	)
target_include_directories(System.Globalization.Native PRIVATE include ../Common)
target_compile_definitions(System.Globalization.Native PRIVATE
	STATIC_ICU=1
	PALEXPORT=
	U_DISABLE_RENAMING
	HAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS
	HAVE_SET_MAX_VARIABLE
	TARGET_UNIX
	)
target_compile_options(System.Globalization.Native PRIVATE "-Wno-reserved-id-macro;-Wno-documentation;-Wno-documentation-unknown-command;-Wno-switch-enum;-Wno-covered-switch-default;-Wno-extra-semi-stmt;-Wno-unknown-warning-option;-pthread")
