# Localization

Auth2Demo uses ASP.NET Core localization and `.resx` resources.

## Supported cultures

- `pt-BR`
- `en-US`

The current default is `en-US`.

## Resource files

```text
Auth2Demo.Web/Resources/SharedResource.resx
Auth2Demo.Web/Resources/SharedResource.en-US.resx
Auth2Demo.Web/Resources/SharedResource.pt-BR.resx
```

`SharedResource.resx` is the neutral fallback. Every user-facing key should be present once in each relevant resource file.

## Culture selection order

1. `UserProfileRequestCultureProvider`.
2. Localization cookie.
3. Query string.
4. `Accept-Language` request header.
5. Default culture.

## User profile

User profile fields can preserve language, culture, locale, country, and time-zone preferences. External providers may supply some of these claims, but the profile remains the authoritative user-editable source.

## Development rules

- Do not hardcode user-facing Portuguese or English in controllers or reusable views.
- Use stable semantic resource keys.
- Keep validation messages localized.
- Do not duplicate resource keys; duplicate XML `<data name="...">` entries are ignored by MSBuild and produce warnings.
- Keep placeholders, button labels, navigation, empty states, and confirmation messages synchronized.
- Test both cultures after changing layout because translated text may be longer.
- Use culture for display formatting, not for persisted numeric or timestamp storage.

## Adding a string

1. Add the key once to the neutral resource.
2. Add the English translation.
3. Add the Brazilian Portuguese translation.
4. Reference the key through `IStringLocalizer<SharedResource>`.
5. Build the Web project to detect resource problems.
6. Test both cultures.
