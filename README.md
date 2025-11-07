**Autor:** Eduard Wagner
**Datum der letzten Änderung:** 2025-11-07

# Kontext

In unserer Systemlandschaft laufen vier Portainer-Umgebungen (**Prod-Int**, **Test-Int**, **Prod-Ext**, **Test-Ext**) mit jeweils eigenem Webservice-Management, das Docker-Container (Image, Start-Parameter, Environment-Vars etc.) pro Umgebung verwaltet.
In einzelnen Umgebungen (z. B. Test-Ext) laufen über 70 Container. Viele dieser Services sind voneinander abhängig und prüfen sich gegenseitig über `/api/test`.
Diese Ketten führen regelmäßig zu hoher Netzlast und teils DDOS-artigem Verhalten.

# Entscheidung

Wir führen **WsPulse** als zentralen HealthCheck-Aggregator ein.
Ziel: zentrale, gecachte Verfügbarkeitsprüfungen und Entlastung der `/api/test`-Endpunkte.

**Aufgaben:**

* Zentrales Polling aller registrierten Services
* Caching der Ergebnisse (Speicher oder MongoDB)
* Verwaltung des Dependency-Graphen (Service → Dependencies)

**REST-API:**

* `GET /api/status` → Gesamtüberblick
* `GET /api/status/{service}` → Einzelstatus (optional inkl. Dependencies)
* `POST /api/register` → Registrierung eines Dienstes und seiner Abhängigkeiten

**Standardisierte Health-Endpunkte der Services:**

* `/api/test` → SelfCheck
* `/api/test/dependencies` → DependencyCheck
* `/api/test/aggregated` → Gesamtstatus
  *(Pfadnamen können variieren, Funktionalität muss konsistent bleiben.)*

**Multi-Instance:**
Pro Portainer-Umgebung läuft eine WsPulse-Instanz:
Prod-Int, Prod-Ext, Test-Int, Test-Ext – jede überwacht nur ihre Umgebung.

**Registrierung (Beispiel):**

```json
POST /api/register
{
  "name": "WsApp",
  "url": "https://api.app.inpro-electric.de",
  "dependencies": [
    "https://api.auth.inpro-electric.de",
    "https://api.ldap.inpro-electric.de",
    "https://api.tagrelation.inpro-electric.de"
  ],
  "environment": "prod-int"
}
```

# Begründung

* Reduktion von Netzlast und Abfragekaskaden
* Zentrale, konsistente Health-Daten
* Keine SPOF durch lokale Fallback-Checks
* Skalierbar durch Multi-Instance-Setup
* Erweiterbar für Monitoring (Dashboards, Alerts)

# Entscheidungsergebnis

* Einführung von WsPulse (ASP.NET Core)
* Lokale `/api/test`-Checks bleiben als Fallback erhalten
* Services registrieren sich bei WsPulse (Self-Discovery)
* MongoDB speichert Service- und Statusdaten
* Pro Umgebung läuft eine eigene WsPulse-Instanz

# Offene Fragen

* TTL/Caching-Intervalle je Service?
* Heartbeat-Frequenz der Services?
* Umgang mit verwaisten Registrierungen?
* Authentifizierung für Registrierung/API?
* Einführung des Status „degraded“?

