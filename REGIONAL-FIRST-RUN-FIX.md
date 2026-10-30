# Initial regional database setup - 3.0.0.5 QA

The console creates default US tables before applying the radio's region on
first run. AddBand2StackTable (IARU2) and AddBandAusStackTable attempted to add
BandStack again, causing DuplicateNameException. Both now reuse the table and
populate regional defaults, as the other region reset routines already do.
Their BandText builders also check actual table existence instead of treating
the refresh flag as evidence that the table is absent.

The BandStackRefresh gate in UpdateRegion is unchanged. Existing user rows are
preserved when refresh is disabled. An explicitly requested BandStack reset
still replaces the entries with defaults, as intended; this is not a migration
that automatically deletes users' stored profiles or memory files.

Regression tests invoke the actual compiled DB code for IARU2 and Australia:
initial US defaults followed by regional setup, repeated refresh without row
growth, preservation with refresh disabled, missing BandStack, and XML round
trip. Tests use synthetic paths, no radio and no user database.
The Map suite now includes RegionalBandStackTests.

For QA, use a backed-up profile and preferably a separate Windows test account
for first-run testing. A new application folder alone does not isolate AppData.
Do not delete an existing database just to work around this exception.
