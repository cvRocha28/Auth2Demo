# Localization

Auth2Demo uses ASP.NET Core localization with `IStringLocalizer` and `.resx` resource files.

## Supported resource files

The Web project contains shared resource files under:

```text
Auth2Demo.Web/Resources
```

Current resource files:

- `SharedResource.resx`
- `SharedResource.en-US.resx`
- `SharedResource.pt-BR.resx`

## Localization approach

Controllers and views should use localized strings instead of hardcoded text whenever the text is user-facing.

Recommended usage:

- Use `IStringLocalizer<SharedResource>` in controllers and services that return UI-facing messages
- Use injected localizer instances in Razor views
- Keep resource keys consistent across all cultures
- Add missing translations to all resource files when adding a new key

## Culture selection

The project includes user profile culture resolution through a request culture provider. This allows the UI language to follow the user profile preference when available.

## Current localization improvements

Recent updates continued the localization effort by moving more user-facing strings to resources and improving English translations.

Areas that should remain localized:

- Account screens
- MFA screens
- Login and authorization screens
- Admin portal labels
- Validation messages
- Success and error messages
- Branding and client management UI

## Best practices

- Do not hardcode Portuguese or English text directly in controllers
- Keep resource keys clear and reusable
- Avoid duplicate keys with slightly different meanings
- Validate both English and Portuguese screens after adding UI changes
- Keep fallback resources complete

## Adding a new localized string

1. Add the key to `SharedResource.resx`
2. Add the English value to `SharedResource.en-US.resx`
3. Add the Portuguese value to `SharedResource.pt-BR.resx`
4. Use the key through `IStringLocalizer<SharedResource>`
5. Test the screen using both supported languages
