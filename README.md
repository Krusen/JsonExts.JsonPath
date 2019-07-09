# JsonExts.JsonPath

[![license](https://img.shields.io/badge/license-Unlicense-blue.svg)](LICENSE.md)
[![NuGet](https://buildstats.info/nuget/JsonExts.JsonPath?includePreReleases=false)](https://www.nuget.org/packages/JsonExts.JsonPath/)

Adds support for mapping properties using JSONPath queries with Newtonsoft.Json.

## Installation


Install the package **JsonExts.JsonPath** from [NuGet](https://www.nuget.org/packages/JsonExts.JsonPath/) 
or install it from the [Package Manager Console](https://docs.microsoft.com/da-dk/nuget/tools/package-manager-console):

```
PM> Install-Package JsonExts.JsonPath
```

## Usage

Add `[JsonPath(string)]` to properties of your model and add `[JsonConverter(typeof(JsonPathObjectConverter))]` to your model.

Here's an example model:

```csharp
[JsonConverter(typeof(JsonPathObjectConverter))]
public class Book
{
    [JsonPath("price")]
    public double Price { get; set; }

    [JsonPath("info.author")]
    public string Author { get; set; }

    [JsonPath("info.related[0].title")]
    public string FirstRelatedTitle { get; set; }

    [JsonPath("info.related[*].title")]
    public List<string> RelatedTitles { get; set; }
}
```

Example JSON being deserialized:

```json
{
    "price": 10.0,
    "info": {
        "title": "Leviathan Wakes",
        "author": "James S. A. Corey",
        "related": [
            { "title": "Caliban's War" },
            { "title": "Abaddon's Gate" },
            { "title": "Cibola Burn" },
            { "title": "Nemesis Games" },
            { "title": "Babylon's Ashes" },
            { "title": "Persepolis Rising" },
            { "title": "Tiamat's Wrath" }
        ]
    }
}
```

Deserialize the JSON to your model like normal using `JsonConvert.DeserializeObject()` or a `JsonSerializer`.

```csharp
var book = JsonConvert.DeserializeObject<Book>(json);
```

Alternatively you can leave out the `[JsonConverter]` attribute on the model and instead specify it when deserializing (or on `JsonSerializerSettings`).

```csharp
public class Book 
{
    ...
}

// Add JsonPathObjectConverter to the collection of converters used
var book = JsonConvert.DeserializeObject<Book>(json, new JsonPathObjectConverter())
```

Serializing `book` back to JSON again will then result in this:

```json
{
    "price": 10.0,
    "author": "James S. A. Corey",
    "firstRelatedTitle": "Caliban's War",
    "relatedTitles": [
        "Caliban's War", 
        "Abaddon's Gate", 
        "Cibola Burn", 
        "Nemesis Games", 
        "Babylon's Ashes", 
        "Persepolis Rising", 
        "Tiamat's Wrath"
    ]
}
```

## Supported Features

- `[JsonConverter]`
  - Supported on both regular properties and combined with `[JsonPath]`.

- `[DefaultValue]`
  - Supported on both regular properties and combined with `[JsonPath]`.

- `[JsonIgnore]`
  - Only supported on regular properties, not supported combined with `[JsonPath]`.

- `[JsonProperty]` features supported when combined with `[JsonPath]`:
  - [X] `ItemConverterType`
  - [X] `NullValueHandling`
  - [X] `DefaultValueHandling`
  - [ ] `NamingStrategyType`
  - [ ] `MissingMemberHandling`
  - [ ] `ReferenceLoopHandling`
  - [ ] `ObjectCreationHandling`
  - [ ] `TypeNameHandling`
  - [ ] `Required`