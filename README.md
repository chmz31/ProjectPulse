# 🧠 ProjectPulse API

**ProjectPulse** es una API RESTful construida con **.NET 8**, **Entity Framework Core (SQLite)** y autenticación **JWT**, diseñada para servir como base para un sistema de gestión de proyectos modular y extensible.

Incluye:

- Autenticación y registro de usuarios con roles.
- Tokens JWT con refresh tokens.
- CRUD básico de proyectos.
- Documentación automática con **Swagger**.

---

## 🚀 Tecnologías principales

| Tecnología | Descripción |
|-------------|--------------|
| [.NET 8](https://dotnet.microsoft.com/) | Plataforma base del proyecto |
| [Entity Framework Core](https://learn.microsoft.com/ef/core/) | ORM con base de datos SQLite |
| [FluentValidation](https://fluentvalidation.net/) | Validación robusta de DTOs |
| [JWT (Json Web Tokens)](https://jwt.io/) | Autenticación segura basada en tokens |
| [Swagger / Swashbuckle](https://swagger.io/tools/swagger-ui/) | Documentación y testeo de la API |
| [Docker](https://www.docker.com/) | Contenedor para despliegue y portabilidad |

---

## 🧩 Estructura del proyecto

```
ProjectPulse/
 ├── ProjectPulse.Api/
 │   ├── Controllers/
 │   ├── DTOs/
 │   ├── Persistence/
 │   ├── Services/
 │   ├── Program.cs
 │   ├── appsettings.Development.json
 │   └── Dockerfile
 └── README.md
```

---

## ⚙️ Variables de entorno

Puedes usar un archivo `.env` en la raíz del proyecto con las siguientes claves:

```env
ConnectionStrings__Default=Data Source=projectpulse.db
Jwt__Issuer=ProjectPulse
Jwt__Audience=ProjectPulse
Jwt__Key=<your-strong-random-secret>
ASPNETCORE_ENVIRONMENT=Production
EnableSwagger=true
```

En producción, proporciona los secretos mediante variables de entorno o un gestor de secretos; no los guardes en archivos versionados.

---

## 🐳 Ejecución con Docker

### 1️⃣ Construir la imagen

```bash
docker build -t projectpulse-api -f ProjectPulse.Api/Dockerfile ProjectPulse.Api
```

### 2️⃣ Ejecutar el contenedor

```bash
docker run -d -p 8080:8080 --name projectpulse   --env-file .env   projectpulse-api
```

### 3️⃣ Verificar que el contenedor está activo

```bash
docker ps
```

Deberías ver algo como:

```
CONTAINER ID   IMAGE              PORTS                   NAMES
xxxxxx         projectpulse-api   0.0.0.0:8080->8080/tcp  projectpulse
```

### 4️⃣ Abrir Swagger

- [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html)

---

## 🧱 Endpoints principales

### 🔐 Autenticación (`/auth`)

| Método | Endpoint | Descripción |
|--------|-----------|-------------|
| POST | `/auth/register` | Registrar nuevo usuario |
| POST | `/auth/login` | Iniciar sesión (retorna `accessToken` y `refreshToken`) |
| POST | `/auth/refresh` | Refrescar tokens JWT |
| POST | `/auth/logout` | Revocar refresh token |

### 📁 Proyectos (`/projects`)

| Método | Endpoint | Descripción |
|--------|-----------|-------------|
| GET | `/projects` | Listar proyectos |
| POST | `/projects` | Crear proyecto |
| GET | `/projects/{id}` | Obtener proyecto por ID |
| PUT | `/projects/{id}` | Editar proyecto |
| DELETE | `/projects/{id}` | Eliminar proyecto |

### 👋 Hello (debug)

| Método | Endpoint | Descripción |
|--------|-----------|-------------|
| GET | `/hello` | Endpoint básico de prueba |

---

## 🧪 Autenticación JWT en Swagger

1. Realiza un **POST** a `/auth/login` con tus credenciales:

   ```json
   {
     "email": "<your-email>",
     "password": "<your-password>"
   }
   ```

2. Copia el valor de `accessToken` del resultado.

3. Pulsa el botón **"Authorize"** arriba a la derecha en Swagger,
   y pega el token con el prefijo:

   ```
   Bearer <accessToken>
   ```

4. Ya puedes acceder a endpoints protegidos como `/projects`.

---

## 🧩 Notas técnicas

- Si ejecutas el contenedor sin volumen, la base de datos se guarda **dentro** del contenedor.
- Para persistir los datos localmente:

  ```bash
  docker run -d -p 8080:8080 --name projectpulse     --env-file .env     -v ${PWD}\data:/app     projectpulse-api
  ```

- Swagger está habilitado tanto en `Development` como en producción si `EnableSwagger=true`.

---

## 🧑‍💻 Autor

**Christian Manrique Zanetti (Chris)**
Software Developer | Game Enthusiast
💼 [LinkedIn](https://www.linkedin.com/in/camz31/)

---

> *“Sic Parvis Magna... Así, de lo pequeño, lo grande”* 💫
> — *Sir Francis Drake*
