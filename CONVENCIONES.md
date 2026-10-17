# Convenciones del proyecto LinkUp Pro

Acuerdos del equipo para que el código de los dos se vea como si lo hubiera escrito una sola persona.
Si algo no está aquí, se decide entre los dos y se agrega a este archivo.

---

## 1. Estructura de la solución

```
LinkUpPro.sln
src/
├── LinkUpPro.Domain        → Entidades y enums. No depende de nada.
├── LinkUpPro.Application   → Interfaces, servicios, DTOs, ViewModels, mapeos.
├── LinkUpPro.Persistence   → DbContext, configuraciones de EF, repositorios, migraciones.
├── LinkUpPro.Shared        → Servicios externos (email, subida de archivos).
└── LinkUpPro.Web           → Controladores, vistas, wwwroot.
```

**Regla de dependencias:** las referencias siempre apuntan hacia Domain.
Domain no referencia a nadie; Web es la única capa que conoce a todas.

---

## 2. Nombres en el código

**Idioma:** el código (clases, métodos, variables, propiedades) va en **inglés**.
Los textos que ve el usuario (vistas, mensajes de error, validaciones) van en **español**.

| Elemento | Formato | Ejemplo |
|---|---|---|
| Clases, métodos, propiedades | PascalCase | `FriendRequest`, `SendRequestAsync()` |
| Variables locales y parámetros | camelCase | `currentUser`, `postId` |
| Campos privados | `_camelCase` | `_postRepository` |
| Interfaces | `I` + PascalCase | `IPostService` |
| Métodos asíncronos | Terminan en `Async` | `GetFriendsAsync()` |
| Constantes | PascalCase | `MaxFileSizeMb` |

### Sufijos por tipo de clase

| Tipo | Sufijo | Capa | Ejemplo |
|---|---|---|---|
| Entidad | *(sin sufijo)* | Domain | `Post`, `Game`, `Ship` |
| DTO | `Dto` | Application | `PostDto`, `CreatePostDto` |
| ViewModel | `ViewModel` | Application | `PostViewModel`, `SavePostViewModel` |
| Servicio | `Service` | Application | `PostService` / `IPostService` |
| Repositorio | `Repository` | Persistence | `PostRepository` / `IPostRepository` |
| Configuración EF | `Configuration` | Persistence | `PostConfiguration` |
| Perfil de AutoMapper | `MappingProfile` | Application | `PostMappingProfile` |
| Controlador | `Controller` | Web | `PostController` |

### ViewModels

- Para **mostrar** datos: `{Entidad}ViewModel` → `PostViewModel`
- Para **crear o editar** (formularios): `Save{Entidad}ViewModel` → `SavePostViewModel`
- Las vistas **nunca** reciben entidades del dominio, solo ViewModels.

### Controladores y acciones

- Un controlador por módulo: `AccountController`, `FriendController`, `PostController`, `GameController`, `NotificationController`, `ProfileController`.
- Acciones estándar: `Index`, `Create`, `Edit`, `Delete`, `Details`.
- Todo `[HttpPost]` lleva `[ValidateAntiForgeryToken]`.
- Todo controlador que requiera sesión lleva `[Authorize]`.

---

## 3. Mensajes de error y validaciones

- Siempre en español, en tono neutro y con la primera letra en mayúscula.
- Terminan en punto.
- Dicen qué está mal y, si se puede, cómo arreglarlo.

| ✅ Bien | ❌ Mal |
|---|---|
| `El correo es obligatorio.` | `Email required` |
| `La contraseña debe tener al menos 8 caracteres.` | `contraseña invalida!!` |
| `Ya enviaste una solicitud a este usuario.` | `Error` |

Las validaciones van en los ViewModels con Data Annotations:

```csharp
[Required(ErrorMessage = "El correo es obligatorio.")]
[EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
public string Email { get; set; }
```

Mensajes de éxito o error después de una acción: con `TempData["Success"]` y `TempData["Error"]`, mostrados en el layout.

---

## 4. Base de datos

- Fechas siempre en **UTC** (`DateTime.UtcNow`).
- Borrado lógico con la propiedad `IsDeleted` en las entidades que lo necesiten.
- Las relaciones y restricciones se configuran en clases `IEntityTypeConfiguration<T>`, no con atributos en las entidades.
- **Migraciones:** solo una persona migra a la vez y avisa por el chat antes de crearla.
  Nombre de la migración en PascalCase y descriptivo: `AddFriendRequests`, `AddIsDeletedToPost`.

---

## 5. Git

### Ramas

| Tipo | Formato | Ejemplo |
|---|---|---|
| Funcionalidad nueva | `feature/{area}-{descripcion}` | `feature/amigos-solicitudes` |
| Corrección | `fix/{descripcion}` | `fix/validaciones` |
| Documentación | `docs/{descripcion}` | `docs/convenciones` |

- Nadie hace push directo a `main`.
- Cada rama dura uno o dos días como máximo.
- Cada mañana: `git pull --rebase origin main`.

### Commits

Formato: `tipo(area): descripción en minúscula y en presente`

| Tipo | Uso |
|---|---|
| `feat` | Funcionalidad nueva |
| `fix` | Corrección de un error |
| `refactor` | Cambio de código que no altera el comportamiento |
| `docs` | Documentación |
| `chore` | Configuración, paquetes, archivos del proyecto |

Ejemplos:
```
feat(amigos): agrega envío de solicitudes de amistad
fix(auth): corrige redirección después del login
docs(readme): agrega pasos para ejecutar el proyecto
```

### Pull Requests

- Título: el mismo de la issue.
- Descripción: `Closes #N` y la lista de criterios de la rúbrica que cubre.
- Lo revisa y aprueba la otra persona.
- Se hace merge con **squash**.
