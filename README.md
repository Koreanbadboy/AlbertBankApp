# MinBankApp
- PINKOD - 1234

## Kom igång
git clone https://github.com/Koreanbadboy/AlbertBankApp.git
cd AlbertBankApp
dotnet run

## Skapa konto
- Skapa nya konton av typen Privatkonto eller Sparkonto
- Sparkonton har en justerbar räntesats

## Ränta på sparkonto
- Applicera ränta direkt på sparkonton  
- Saldot uppdateras automatiskt utifrån aktuell räntesats

## Överföringar mellan egna konton
- Gör smidiga överföringar mellan dina egna konton  
- Varje transaktion sparas i historiken med datum och belopp

## Hantera konto från startsidan
- Utför insättningar och uttag direkt från kontolistan  
- Se uppdaterat saldo och senaste transaktion i realtid

## Historik
- Visa alla transaktioner för alla eller ett utvalt konto  
- Innehåller datum, typ, belopp, motpart och notering

## Filtrering & sökning
- Filtrera historik på datum och belopp  
- Sök transaktioner inom valfri datumintervall för bättre översikt

# Backup (export/import)
- Möjlighet att exportera data som JSON
- Importera tidigare data med validering av struktur och innehåll
- Säkerställer att inga ogiltiga eller korrupta poster läses in

## Logga ut
- Loggar ut från appen


## Tekniska VG-val och motiveringar

------------------------------------------------------------

### Pinkod (inloggning):
Jag valde att använda en enkel pinkodslåsning som en visuell spärr i gränssnittet.  
Syftet är att simulera en inloggningssituation utan att hantera riktiga användare eller autentisering.  
Det ger en tydlig startpunkt innan användaren får tillgång till sina konton och kan enkelt byggas ut till en riktig inloggning i framtiden.

------------------------------------------------------------

### Ränta på sparkonto:
Jag valde att lägga till en justerbar ränta på sparkonton för att ge användaren möjlighet att simulera hur saldot växer över tid.  
Funktionen gör applikationen mer realistisk och visar hur finansiell logik kan tillämpas på ett tydligt och användarvänligt sätt.

------------------------------------------------------------

### JSON export/import med validering:
Jag valde att använda JSON för att exportera och importera kontodata.  
Formatet är enkelt, standardiserat och passar bra för backup och delning.  
Vid import sker validering av t.ex. kontotyp, transaktions-ID och belopp för att undvika felaktig eller korrupt data.  
Det förbättrar användarens möjlighet att säkerhetskopiera sin information på ett säkert sätt.
