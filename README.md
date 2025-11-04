# 🚀 Censudex - Servicio de Autenticación (Taller 2)

Este proyecto es el **Servicio de Autenticación**. Su trabajo es:

1.  Recibir un login (`usuario` + `password`).
2.  Verificar los datos llamando al `ClientsService` (vía gRPC).
3.  Crear y firmar **JSON Web Tokens (JWT)**.
4.  Manejar el `logout` y `validate-token` usando una **Blacklist en Redis**.

---

## ⚠️ ¡Requisito SÚPER Importante!

Este servicio **NO FUNCIONA SOLO**. Antes de iniciarlo, necesitas tener **DOS** servicios corriendo en tu máquina:

1.  La base de datos **Redis** (para la Blacklist).
2.  El **`ClientsService`** 

---

## ⚙️ Paso 1: Inicia las Dependencias Redis y ClientsService

Abre dos terminales y deja estos servicios corriendo.

**Terminal 1: Inicia Redis para la Blacklist**

```bash
# Si es la primera vez que lo corres:
docker run -d --name censudex-redis -p 6379:6379 redis:latest

# Si ya lo habías creado y solo está detenido:
docker start censudex-redis
```

**Terminal 2: Inicia el ClientsService**

```bash
# Ve a la carpeta raíz de tu censudex-clients-service
cd ruta/a/censudex-clients-service/ClientsService

# Inicia el servicio
dotnet run
```

Fíjate en la URL que te da (ej. http://localhost:5186).

## ⚙️ Paso 2: Configura este Proyecto appsettings.json

1.  Abre el archivo AuthService/appsettings.json de este proyecto.

2.  Asegúrate de que la URL de ClientsService coincida con la del Paso 1:
    ```JSON
    "ServiceUrls": {
    "ClientsService": "http://localhost:5186"
    }
    ```
3.  (Opcional) Cambia la llave secreta del JWT:

    ```JSON
    "Jwt": {
        "Key":      "ESTE_ES_UN_SECRETO_MUY_LARGO_Y_SEGURO_QUE_DEBES_CAMBIAR"
    }
    ```
## ⚙️ Paso 3: ¡Inicia el AuthService!
Ahora que sus dependencias están corriendo, puedes iniciar este servicio.

1. Abre una tercera terminal en la raíz de este proyecto (censudex-auth-service).

2. Entra en la carpeta del servicio:
    ```bash
    cd AuthService
    ```
3. (Opcional pero recomendado) Descarga los paquetes:
    ```bash
    dotnet restore
    ```
4. Inicia el servicio:
   ```bash
    dotnet run
    ``` 
5. ¡Listo! Verás un mensaje como: Now listening on: http://localhost:5132

## 🧪 Guía de Pruebas Postman
* **Método:** `POST`
* **URL:** `http://localhost:5186/Auth/login` (usa tu puerto)
* **Body:** (raw, JSON)
    ```json
    {
      "emailOrUsername": "admin",
      "password": "Admin123!"
    }
    ```
* **Resultado:** Recibirás un `200 OK` con un token.
    ```json
    {
      "token": "eyJh..."
    }
    ```
* **¡COPIA ESE TOKEN!**

### 2. `GET /validate-token`
* **Método:** `GET`
* **URL:** `http://localhost:5186/Auth/validate-token` (usa tu puerto)
* **Auth:** Pestaña `Authorization` > Tipo `Bearer Token` > Pega el token que copiaste.
* **Resultado:** `200 OK` con `{"valid": true, ...}`.

### 3. `POST /logout`
* **Método:** `POST`
* **URL:** `http://localhost:5186/Auth/logout`
* **Auth:** Pestaña `Authorization` > Tipo `Bearer Token` > Pega el mismo token.
* **Resultado:** `200 OK` con `{"message": "Logged out successfully"}`.

### 4. Prueba Final (Verificar Blacklist)
* Vuelve a la pestaña de `GET /validate-token`.
* Presiona `Send` otra vez (con el mismo token).
* **Resultado:** ¡Ahora recibirás un `401 Unauthorized` con el mensaje `{"message": "Token has been revoked"}`!