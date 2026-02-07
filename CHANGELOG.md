# Changelog

## 2026-02-07

### Changed
- **Home Page Empty State**: Replaced icon with disclaimer InfoBar reminding users that KeyPocket is not a professional key management solution
- **Provider Settings UI**: Added count badges (InfoBadge) to API Keys and Models section headers, and optimized sticky headers to only apply to these two sections
- **General Settings Layout**: Optimized layout by combining "API Mode" and "Base URL" into a single row (3:7 ratio) for better space utilization
- **Localization**: Added multi-language support (en-US, zh-CN, zh-TW) for Import button and CSV operation menu items
- **Provider Icons**: Added a collection of preset provider icons and fixed an issue where they were not appearing in the app due to missing project inclusion

## 2026-02-06

### Added
- **CSV Import for Models**: Added CSV import and template generation functionality to Provider Settings page. Users can now bulk import model configurations via CSV files with support for ModelId, Name, InputPrice, OutputPrice, and Type fields. Includes Upsert logic (update existing or insert new), robust CSV parsing with quoted field support, and validation with error reporting.
  - Template examples use `eg:` prefix to prevent accidental imports
  - Import automatically skips rows with `eg:` prefix
  - Prices are automatically rounded to 3 decimal places
  - Import results displayed via InfoBar with severity levels (Success/Info/Warning/Error)
- **Incremental Model Refresh**: Created `RefreshModels()` function that preserves editing state when refreshing model lists, preventing disruption to users actively editing cards

