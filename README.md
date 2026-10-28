# LinkUp Pro

Red social desarrollada en ASP.NET Core MVC con arquitectura en capas. Permite registrarse y activar la cuenta por correo, gestionar amigos y solicitudes de amistad, crear publicaciones con texto, imagen o video de YouTube, comentar y reaccionar, recibir notificaciones y jugar partidas de Battleship contra amigos.

## Colaboradores

| Nombre | Matrícula | Seccion | GitHub |
|---|---|---|---|
| RONEL MAMBRU FIDEL | 2024-2438 | Seccion 6 | [@Ronelmambru6](https://github.com/Ronelmambru6) |
| IBN MOISES FRIAS GOMEZ | 2024-2504 | Seccion 6 | [@IbnFrias](https://github.com/IbnFrias) |

## Requisitos

| Herramienta | Versión | Notas |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | 9.0.100 o superior (9.x) | Fijado en `global.json` |
| SQL Server | LocalDB, Express o Developer | LocalDB viene con Visual Studio (carga de trabajo "Desarrollo de ASP.NET y web") |
| Herramienta `dotnet-ef` | 9.x | Solo si se usa la terminal; en Visual Studio se puede usar la Consola del Administrador de Paquetes |
| Git | Cualquier versión reciente | |
| Cuenta de Gmail con contraseña de aplicación | — | Para enviar los correos de activación y recuperación de contraseña |

Instalar `dotnet-ef` (una sola vez):

```bash
dotnet tool install --global dotnet-ef --version 9.*
```

Si ya hay otra versión instalada (por ejemplo la 10), desinstalarla antes con `dotnet tool uninstall --global dotnet-ef`. Si después la terminal no encuentra `dotnet ef`, agregar la carpeta de herramientas al PATH: en Linux, macOS o Git Bash es `~/.dotnet/tools` (`export PATH="$PATH:$HOME/.dotnet/tools"`); en Windows es `%USERPROFILE%\.dotnet\tools`.

## Pasos para ejecutar

### 1. Clonar el repositorio

```bash
git clone https://github.com/Ronelmambru6/Project-LinkUp.git
cd Project-LinkUp
```

### 2. Revisar la cadena de conexión

Por defecto el proyecto usa SQL Server LocalDB y crea la base `LinkUpProDb`. Está en `src/LinkUpPro.Web/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=LinkUpProDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

Si se usa otra instancia de SQL Server (por ejemplo SQL Express), cambiar solo el valor de `Server`, por ejemplo `Server=localhost\\SQLEXPRESS`.

**Linux o macOS:** LocalDB solo existe en Windows. Se puede levantar SQL Server con Docker (o Podman):

```bash
docker run -d --name linkup-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=LinkUp_Dev123!" \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

Y guardar la cadena de conexión con user-secrets, sin tocar `appsettings.json`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=localhost,1433;Database=LinkUpProDb;User Id=sa;Password=LinkUp_Dev123!;TrustServerCertificate=True;MultipleActiveResultSets=true" \
  --project src/LinkUpPro.Web
```

### 3. Configurar el correo (user-secrets)

El usuario y la contraseña del correo **no** están en el repositorio. Se guardan con user-secrets en la máquina de cada uno:

```bash
dotnet user-secrets set "MailSettings:User" "tu_correo@gmail.com" --project src/LinkUpPro.Web
dotnet user-secrets set "MailSettings:Password" "tu_contraseña_de_aplicacion" --project src/LinkUpPro.Web
```

La contraseña debe ser una **contraseña de aplicación** de Google (Cuenta de Google → Seguridad → Verificación en dos pasos → Contraseñas de aplicaciones), no la contraseña normal del correo. Si se usa un correo distinto al configurado, cambiar también `MailSettings:From` en `appsettings.json` o con user-secrets.

Sin este paso la aplicación arranca, pero no podrá enviar el correo de activación y los usuarios nuevos no podrán iniciar sesión.

### 4. Crear la base de datos con las migraciones

Las migraciones ya están incluidas en el repositorio (`src/LinkUpPro.Persistence/Migrations`). Solo hay que aplicarlas.

Desde la terminal, en la raíz del repositorio:

```bash
dotnet ef database update --project src/LinkUpPro.Persistence --startup-project src/LinkUpPro.Web
```

Desde Visual Studio (Herramientas → Administrador de paquetes NuGet → Consola del Administrador de paquetes), con **Proyecto predeterminado = `LinkUpPro.Persistence`** y `LinkUpPro.Web` como proyecto de inicio:

```powershell
Update-Database
```

### 5. Cargar los datos de prueba (seed)

El seed crea usuarios, amistades, solicitudes, publicaciones, comentarios, reacciones, notificaciones y partidas de Battleship para poder probar todas las pantallas sin registrar nada a mano. Se ejecuta al arrancar la aplicación con el perfil `https-seed` (que pasa el argumento `--seed`). Si todavía no se aplicaron las migraciones, el seed las aplica primero.

```bash
dotnet run --project src/LinkUpPro.Web --launch-profile https-seed
```

En Visual Studio basta con elegir el perfil **https-seed** en la lista junto al botón de inicio y presionar F5.

Es seguro repetirlo: si el usuario `ana` ya existe, no hace nada. Para volver a cargarlo desde cero, borrar la base (ver [Notas](#notas)) y ejecutar de nuevo. Los datos se guardan en una sola transacción: si algo falla, no queda la base a medias.

**Cuentas de prueba** (todas con la contraseña `Prueba123!`; se inicia sesión con el nombre de usuario):

| Usuario | Nombre | Qué sirve para probar |
|---|---|---|
| `ana` | Ana Pérez | Cuenta principal. Amiga de carlos, maria y luis; tiene una solicitud pendiente de pedro y otra enviada a sofia |
| `carlos` | Carlos Rodríguez | Amigo de ana, maria y sofia |
| `maria` | María Gómez | Amiga de ana y carlos; tiene una partida esperando a que ana posicione |
| `luis` | Luis Fernández | Amigo de ana y sofia; partida en curso con ana |
| `sofia` | Sofía Martínez | **No** es amiga de ana (se ve su comentario sin enlace al perfil); tiene una partida sin barcos posicionados |
| `pedro` | Pedro Núñez | Sin amigos; solicitud enviada a ana |
| `diego` | Diego Jiménez | Cuenta **sin activar** (para probar el mensaje de cuenta inactiva) |

**Qué incluye:**

- **Amigos:** 6 amistades activas, solicitudes aceptadas, pendientes, rechazada y cancelada.
- **Publicaciones:** 12, con imagen o enlace de YouTube; entre ellas una editada, una eliminada, una "Solo yo" y una con comentarios desactivados.
- **Comentarios:** respuestas anidadas, un comentario editado, uno eliminado con respuestas ("Este comentario fue eliminado.") y otro eliminado sin respuestas.
- **Reacciones y notificaciones:** reacciones de Me gusta y No me gusta, y las notificaciones que generaría la aplicación (leídas y no leídas, incluso de contenido eliminado).
- **Battleship:** una partida ganada por hundimiento, una por rendición, una en curso, y dos en posicionamiento.

Las imágenes de prueba están en `src/LinkUpPro.Web/wwwroot/seed/`.

### 6. Ejecutar la aplicación

```bash
dotnet run --project src/LinkUpPro.Web --launch-profile https
```

O abrir `LinkUpPro.sln` en Visual Studio, marcar `LinkUpPro.Web` como proyecto de inicio y presionar F5.

La aplicación queda disponible en:

- https://localhost:7251
- http://localhost:5193

Si el navegador avisa que el certificado no es de confianza, ejecutar una vez `dotnet dev-certs https --trust`.

## Prueba desde cero (máquina limpia)

Lista para verificar el proyecto como lo haría quien lo clona por primera vez:

1. `git clone https://github.com/Ronelmambru6/Project-LinkUp.git` y entrar a la carpeta.
2. `dotnet restore` (sin errores).
3. `dotnet build` (compila sin errores ni advertencias nuevas).
4. Configurar el correo con user-secrets (paso 3).
5. `dotnet run --project src/LinkUpPro.Web --launch-profile https-seed` (aplica las migraciones y carga el seed).
6. Entrar a https://localhost:7251 con `ana` / `Prueba123!` y comprobar: Home con publicaciones, Amigos, Solicitudes, Notificaciones con contador y Battleship con partidas activas e historial.
7. Registrar un usuario nuevo y confirmar que llega el correo de activación.

## Estructura de la solución

```
LinkUpPro.sln
src/
├── LinkUpPro.Domain        → Entidades y enums. No depende de nada.
├── LinkUpPro.Application   → Interfaces, servicios, DTOs, ViewModels y mapeos (AutoMapper).
├── LinkUpPro.Persistence   → DbContext, configuraciones de EF Core, repositorios, Identity y migraciones.
├── LinkUpPro.Shared        → Servicios externos: envío de correo (MailKit) y almacenamiento de imágenes.
└── LinkUpPro.Web           → Controladores, vistas y archivos estáticos.
```

Las convenciones de código, ramas y commits del equipo están en [CONVENCIONES.md](CONVENCIONES.md).

## Tecnologías

- ASP.NET Core MVC (.NET 9)
- Entity Framework Core 9 con SQL Server
- ASP.NET Core Identity
- AutoMapper
- MailKit

## Notas

- Las imágenes que suben los usuarios se guardan en `src/LinkUpPro.Web/wwwroot/uploads/`. La carpeta se crea sola y está excluida de Git.
- Para empezar desde cero, borrar la base y volver a aplicar las migraciones:

  ```bash
  dotnet ef database drop --project src/LinkUpPro.Persistence --startup-project src/LinkUpPro.Web
  dotnet ef database update --project src/LinkUpPro.Persistence --startup-project src/LinkUpPro.Web
  ```
