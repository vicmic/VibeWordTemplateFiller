# Google Distance Calculator

Applicazione WPF (.NET 10) per calcolare le distanze tra coppie di indirizzi usando le API di Google Maps, leggendo i dati da file Excel e salvando i risultati nello stesso file.

## Requisiti

- **Windows 10/11** (WPF è solo Windows)
- **.NET 10 SDK** — scarica da https://dotnet.microsoft.com/download/dotnet/10.0
- **Chiave API Google Maps** con il servizio **Distance Matrix API** abilitato

## Compilazione ed esecuzione

```bash
# Ripristina i pacchetti e compila
dotnet restore DistanceCalculator/DistanceCalculator.csproj
dotnet build DistanceCalculator/DistanceCalculator.csproj -c Release

# Oppure avvia direttamente
dotnet run --project DistanceCalculator/DistanceCalculator.csproj
```

Oppure apri `DistanceCalculator.sln` in Visual Studio 2022+ e premi F5.

## Formato file Excel

Il file Excel deve avere le seguenti colonne (configurabili nell'app):

| Colonna predefinita | Contenuto                      | Obbligatoria |
|---------------------|-------------------------------|-------------|
| A                   | Indirizzo di partenza         | Sì          |
| B                   | Indirizzo di arrivo           | Sì          |
| C                   | Descrizione sito partenza     | No          |
| D                   | Descrizione sito arrivo       | No          |

La colonna **Distanza (km)** viene scritta automaticamente dopo le colonne usate.  
In fondo al foglio viene aggiunto il **totale km** percorsi.

## Come ottenere la chiave API

1. Vai su [Google Cloud Console](https://console.cloud.google.com/)
2. Crea un progetto o selezionane uno esistente
3. Abilita **Distance Matrix API**
4. Crea una credenziale **API Key** e copiala nell'app

## Funzionalità

- Drag & drop del file Excel o selezione tramite dialogo
- Rilevamento automatico delle colonne (basato sui nomi delle intestazioni)
- Validazione della chiave API prima di avviare
- Progress bar in tempo reale con stato per ogni riga
- Gestione errori per indirizzi non trovati, limite quote, ecc.
- Salvataggio con formattazione colorata nel file Excel originale
- La chiave API viene salvata in `%APPDATA%\DistanceCalculator\settings.json`

## Dipendenze NuGet

| Pacchetto                 | Versione  | Uso                              |
|--------------------------|-----------|----------------------------------|
| MaterialDesignThemes      | 5.1.0     | UI Material Design 3             |
| ClosedXML                 | 0.104.2   | Lettura/scrittura file Excel     |
| CommunityToolkit.Mvvm     | 8.4.0     | Pattern MVVM (source generators) |
