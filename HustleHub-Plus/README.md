# HustleHub+

HustleHub+ is a secure freelance marketplace developed for the INSY7314 Portfolio of Evidence. The platform is intended to provide freelancers and clients with a secure environment for managing freelance services and interactions.

## Part 1: Secure Backend Foundations

Part 1 focuses on establishing the secure backend foundation of the system using Node.js and Express.js. The implementation includes secure user registration and login, password protection, JSON Web Token (JWT) authentication, HTTPS communication, input validation, controlled error handling and API testing.

## Intended Users

HustleHub+ is designed for:

- Freelancers who want to offer services.
- Clients who want to find and interact with freelancers.
- Users who require a secure and reliable freelance marketplace.

## Technology Stack

### Backend
- Node.js
- Express.js

### Security and Validation
- bcrypt
- JSON Web Tokens (JWT)
- express-validator
- express-rate-limit
- Helmet
- CORS
- HTTPS
- dotenv

### Development
- Nodemon
- Git and GitHub

### Testing
- Postman

## Project Structure

```text
HustleHub-Plus/
│
├── docs/
│   ├── architecture/
│   └── screenshots/
│
├── src/
│   ├── controllers/
│   ├── data/
│   ├── middleware/
│   ├── routes/
│   └── utils/
│
├── certs/                              # Local only - ignored by Git
├── .env                                # Local only - ignored by Git
├── .env.example
├── .gitignore
├── HustleHub_Part1_Postman_Collection.json
├── package-lock.json
├── package.json
├── README.md
└── server.js
```

## Installation and Setup

### Prerequisites

Before running HustleHub+, ensure that the following are available:

- Node.js
- npm
- Git
- Postman
- A local SSL certificate for HTTPS testing

### Clone the Repository

Clone the repository and navigate into the project directory:

```bash
git clone <repository-url>
cd HustleHub-Plus
```

### Install Dependencies

Install the required Node.js packages:

```bash
npm install
```

### Environment Configuration

Create a `.env` file in the root of the project. The required environment variables are shown in `.env.example`.

Example:

```env
PORT=3000
JWT_SECRET=your_secure_jwt_secret
JWT_EXPIRES_IN=1h
SSL_PASSPHRASE=your_ssl_certificate_passphrase
```

The `.env` file must not be committed to GitHub because it may contain sensitive configuration information such as JWT secrets and certificate passphrases.

### HTTPS Certificate

HustleHub+ uses HTTPS during local development.

Each developer must create a local certificate and store it using the following structure:

```text
certs/
└── localhost.pfx
```

The `certs` directory is excluded from version control because certificates and private keys must not be stored in the repository.

The backend expects the certificate at:

```text
certs/localhost.pfx
```

### Run the Application

Start the backend in development mode:

```bash
npm run dev
```

The API will run at:

```text
https://localhost:3000
```

The development server uses Nodemon so that the application automatically restarts when source files are changed.

---

## API Endpoints

### Health Check

**Method:** `GET`

**Endpoint:**

```text
/
```

**Authentication:** Not required

This endpoint confirms that the HustleHub+ backend is running successfully.

A successful request returns HTTP status `200 OK`.

---

### Register User

**Method:** `POST`

**Endpoint:**

```text
/api/auth/register
```

**Authentication:** Not required

Registers a new freelancer or client account.

Example request:

```json
{
  "username": "testuser",
  "email": "test@example.com",
  "password": "Password1!",
  "confirmPassword": "Password1!",
  "role": "freelancer"
}
```

A successful registration returns HTTP status `201 Created`.

Registration validates the supplied username, email address, password, password confirmation and user role. Duplicate email addresses and usernames are rejected.

The user's password is hashed before being stored and is never returned in an API response.

Possible responses include:

| Status | Meaning |
| --- | --- |
| `201 Created` | User registered successfully |
| `400 Bad Request` | Registration input failed validation |
| `409 Conflict` | Email address or username is already registered |
| `429 Too Many Requests` | Rate limit has been exceeded |
| `500 Internal Server Error` | An unexpected server error occurred |

---

### Login

**Method:** `POST`

**Endpoint:**

```text
/api/auth/login
```

**Authentication:** Not required

Authenticates an existing HustleHub+ user.

Example request:

```json
{
  "email": "test@example.com",
  "password": "Password1!"
}
```

A successful login returns HTTP status `200 OK` together with the authenticated user's information and a JSON Web Token.

The JWT can then be supplied to protected endpoints using the HTTP `Authorization` header.

Example:

```text
Authorization: Bearer <token>
```

Possible responses include:

| Status | Meaning |
| --- | --- |
| `200 OK` | Login successful and JWT generated |
| `400 Bad Request` | Login input failed validation |
| `401 Unauthorized` | Email address or password is incorrect |
| `429 Too Many Requests` | Rate limit has been exceeded |
| `500 Internal Server Error` | An unexpected server error occurred |

---

### View User Profile

**Method:** `GET`

**Endpoint:**

```text
/api/auth/profile
```

**Authentication:** Required

This protected endpoint returns the profile of the currently authenticated user.

The request must contain a valid JWT:

```text
Authorization: Bearer <token>
```

Possible responses include:

| Status | Meaning |
| --- | --- |
| `200 OK` | Authenticated profile returned successfully |
| `401 Unauthorized` | Authentication token is missing, invalid or expired |
| `404 Not Found` | The authenticated user no longer exists |

---

## Data Storage

Part 1 of HustleHub+ uses an in-memory user store rather than a permanent database.

The user data layer is contained within:

```text
src/data/users.js
```

This approach is suitable for the secure backend foundation required during Part 1 and allows the registration, login and authentication features to be demonstrated without introducing the full database implementation prematurely.

Because the data is stored in memory, registered users are removed whenever the Node.js server is restarted.

Permanent database storage using the MERN architecture will be introduced during the later development stages of HustleHub+.

---

## Security Implementation

Security is a central requirement of the HustleHub+ backend. Multiple controls have been implemented to reduce common authentication, data exposure and API security risks.

### Password Hashing

Passwords are never stored in plaintext.

HustleHub+ uses `bcrypt` to generate a salted cryptographic hash before a password is stored. During login, the supplied password is compared against the stored hash rather than directly comparing plaintext passwords.

This reduces the risk of user credentials being exposed if stored user information is compromised.

Password hashes are also removed from user objects before any user information is returned through the API.

### Password Validation

Registration passwords must satisfy minimum security requirements, including:

- At least eight characters
- At least one uppercase letter
- At least one lowercase letter
- At least one number
- At least one special character

A password confirmation field is also required during registration to reduce errors when creating an account.

### JSON Web Token Authentication

HustleHub+ uses JSON Web Tokens (JWT) to authenticate requests to protected API resources.

After a successful login, the backend signs and returns a JWT containing the user's identifier, email address and role.

The token has a limited lifetime and must be supplied using the Bearer authentication scheme when accessing protected routes.

Protected endpoints verify the JWT before allowing the request to continue. Requests containing missing, invalid or expired tokens are rejected.

### HTTPS

The backend is hosted locally using HTTPS rather than unencrypted HTTP.

A self-signed certificate is used during development. HTTPS encrypts communication between the client and API, reducing the risk of sensitive information such as credentials and authentication tokens being exposed while travelling across a network.

Certificate files and their passphrases are excluded from version control.

### Helmet

The Express application uses Helmet middleware to configure security-related HTTP response headers.

These headers provide additional protection against several common web-based attacks and reduce unnecessary exposure of server information.

### CORS

Cross-Origin Resource Sharing (CORS) middleware is configured within the Express application.

CORS provides control over how external clients and origins can interact with the API and forms part of the backend's request security configuration.

### Input Validation

User-controlled registration and login input is validated before it reaches the main controller logic.

The project uses `express-validator` to verify values such as usernames, email addresses, passwords and roles.

Invalid requests are rejected with controlled `400 Bad Request` responses instead of being processed by the application.

Input validation reduces the likelihood of malformed or inappropriate data reaching the business logic layer.

### Rate Limiting

Authentication endpoints are protected using rate limiting.

The current configuration limits repeated authentication requests from the same IP address within a defined time window.

Requests that exceed the allowed limit receive HTTP status `429 Too Many Requests`.

Rate limiting helps reduce brute-force login attempts and abuse of authentication endpoints.

### Centralised Error Handling

HustleHub+ uses centralised Express error-handling middleware.

Operational errors are converted into controlled JSON responses containing an appropriate HTTP status code and safe error message.

Unexpected server errors return a generic response rather than exposing internal implementation details.

Production responses do not expose stack traces, file paths or other information that could assist an attacker.

### Unknown Routes

Requests to API routes that do not exist are passed to a dedicated not-found handler.

This produces a consistent `404 Not Found` JSON response before forwarding the error through the central error-handling system.

---

## Backend Structure

The backend follows a modular structure to separate different responsibilities.

### Controllers

```text
src/controllers/
```

Controllers contain the main request-processing logic for operations such as registration, login and retrieving an authenticated user's profile.

### Data

```text
src/data/
```

The data layer currently contains the temporary in-memory user store used during Part 1.

### Middleware

```text
src/middleware/
```

Middleware is responsible for functionality that operates before or after the main controllers, including:

- Registration validation
- Login validation
- Rate limiting
- 404 handling
- Centralised error handling

### Routes

```text
src/routes/
```

Routes define the available API endpoints and connect each endpoint to its required validation, authentication and controller logic.

### Utilities

```text
src/utils/
```

Reusable utilities support functions such as:

- Password hashing and comparison
- JWT creation and verification
- Operational application errors
- Asynchronous error forwarding

This separation improves maintainability by preventing authentication, validation, routing and error-handling responsibilities from being placed within a single file.

---

## API Testing

Postman is used to test the HustleHub+ Part 1 backend.

The repository contains the Part 1 Postman collection:

```text
HustleHub_Part1_Postman_Collection.json
```

Testing covers both successful and unsuccessful requests, including:

- Successful user registration
- Duplicate user registration
- Invalid registration input
- Successful login
- Incorrect login credentials
- JWT generation
- Access to protected routes with a valid token
- Access to protected routes without authentication
- Invalid or expired authentication tokens
- Unknown API routes
- Malformed request data
- Controlled error responses
- Rate limiting

Testing both positive and negative scenarios ensures that the API does not only work when valid data is supplied, but also responds securely and predictably when invalid or malicious requests are made.

## API Testing Evidence

The following screenshots provide evidence of successful and unsuccessful API requests tested against the final integrated Part 1 backend.

### Health Check — 200 OK

![Health Check](docs/screenshots/01-health-check-200.png)

### Successful Registration — 201 Created

![Successful Registration](docs/screenshots/02-registration-success-201.png)

### Duplicate Registration — 409 Conflict

![Duplicate Registration](docs/screenshots/03-registration-duplicate-409.png)

### Registration Validation — 400 Bad Request

![Registration Validation](docs/screenshots/04-registration-validation-400.png)

### Successful Login and JWT Generation — 200 OK

![Successful Login](docs/screenshots/05-login-success-jwt-200.png)

### Failed Login — 401 Unauthorized

![Failed Login](docs/screenshots/06-login-failed-401.png)

### Authorized Protected Profile — 200 OK

![Authorized Profile](docs/screenshots/07-profile-authorized-200.png)

### Unauthorized Protected Profile — 401 Unauthorized

![Unauthorized Profile](docs/screenshots/08-profile-unauthorized-401.png)

### Malformed JSON Handling — 400 Bad Request

![Malformed JSON](docs/screenshots/09-malformed-json-400.png)

### Unknown Route Handling — 404 Not Found

![Unknown Route](docs/screenshots/10_unknown_route_404.png)
---

## System Architecture

HustleHub+ is designed around the MERN architecture:

- **MongoDB** will provide persistent data storage during later implementation stages.
- **Express.js** provides the backend web framework and API routing.
- **React** will provide the client-side interface during the full-stack development stage.
- **Node.js** provides the backend JavaScript runtime.

Part 1 focuses on the secure Node.js and Express.js backend foundation. The architecture has been designed so that the temporary in-memory data layer can later be replaced by MongoDB without requiring the authentication and routing structure to be completely redesigned.

The system architecture diagram documents the major application components, system boundaries and security mechanisms used by HustleHub+.

### Architecture Diagram

The diagram below illustrates the Part 1 backend architecture, system boundaries, authentication flow and implemented security mechanisms, while also showing how the backend will form part of the complete MERN architecture during later development stages.

![HustleHub+ Part 1 System Architecture](docs/architecture/hustlehub-part1-architecture.png)
---

## Part 1 Demonstration

The Part 1 demonstration will show the secure backend running through HTTPS and demonstrate the primary authentication workflow.

The demonstration includes:

1. Starting the HustleHub+ backend.
2. Confirming that the API is available.
3. Registering a valid user.
4. Demonstrating registration validation and invalid requests.
5. Logging in using the registered account.
6. Showing the JWT generated after successful authentication.
7. Using the JWT to access the protected profile endpoint.
8. Demonstrating rejection of an unauthenticated request.
9. Demonstrating controlled error handling.

---

## Contributors

HustleHub+ was developed collaboratively for the INSY7314 Portfolio of Evidence.

| Member | Part 1 Responsibilities |
| --- | --- |
| Mikhaeel | Group leadership, initial Node/Express project setup, HTTPS configuration, GitHub management, integration and debugging, README documentation and final testing |
| Mangaliso | User registration, password hashing, registration validation and user-data structure |
| Julian | User login, JWT authentication, authentication middleware and protected profile functionality |
| Theolin | Centralised error handling, 404 handling, rate limiting, Postman testing collection and system architecture diagram |

Each member contributed through their own development work and Git commit history. Feature branches and an integration branch were used to combine and test the individual components before finalisation.