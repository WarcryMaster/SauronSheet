
# Especificación de Feature: Estilizado visual atractivo para Login con Tailwind

**Rama de Feature**: `001-login-tailwind-style`
**Creado**: 2026-03-07
**Estado**: Borrador
**Input**: Añadir un aspecto visual atractivo usando Tailwind para la página de login en src/SauronSheet.Frontend/Pages/Auth

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Login visualmente atractivo (Prioridad: P1)

Como usuario nuevo o recurrente,
quiero que la página de login tenga un diseño moderno, profesional y agradable visualmente,
para sentir confianza y facilidad al iniciar sesión.

**Por qué esta prioridad**: El login es la puerta de entrada a la aplicación y la primera impresión para el usuario. Un diseño atractivo mejora la percepción de calidad y la tasa de conversión.

**Prueba independiente**: Puede probarse accediendo a /Auth/Login y evaluando la experiencia visual y de usabilidad sin necesidad de otras páginas.

**Escenarios de aceptación:**

1. **Dado** que accedo a /Auth/Login, **Cuando** la página carga, **Entonces** veo un formulario centrado, con colores, tipografía y componentes estilizados con Tailwind, y un fondo agradable.
2. **Dado** que la página de login está cargada, **Cuando** visualizo en móvil, **Entonces** el diseño es responsivo y se adapta correctamente.
3. **Dado** que la página de login está cargada, **Cuando** hay un error de autenticación, **Entonces** el mensaje de error se muestra de forma clara y visualmente integrada.

---

### Historia de Usuario 2 - Consistencia visual con la marca (Prioridad: P2)

Como usuario,
quiero que el login use los colores y estilos base definidos para la marca,
para tener una experiencia coherente con el resto de la aplicación.

**Por qué esta prioridad**: La coherencia visual refuerza la identidad y confianza del usuario.

**Prueba independiente**: Puede evaluarse comparando el login con otras páginas principales (Dashboard, Register) y verificando la consistencia de colores, botones y tipografía.

**Escenarios de aceptación:**

1. **Dado** que accedo a /Auth/Login, **Cuando** visualizo los botones y campos, **Entonces** los estilos coinciden con los del Dashboard y otras páginas clave.

---

### Historia de Usuario 3 - Accesibilidad y usabilidad (Prioridad: P3)

Como usuario con necesidades de accesibilidad,
quiero que el login sea navegable por teclado y legible por lectores de pantalla,
para poder iniciar sesión sin barreras.

**Por qué esta prioridad**: La accesibilidad es un requisito legal y de calidad para todos los usuarios.

**Prueba independiente**: Puede probarse usando solo teclado y herramientas de accesibilidad (como Lighthouse o axe).

**Escenarios de aceptación:**

1. **Dado** que accedo a /Auth/Login, **Cuando** navego con el teclado, **Entonces** puedo acceder a todos los campos y botones sin problemas.
2. **Dado** que accedo a /Auth/Login, **Cuando** uso un lector de pantalla, **Entonces** los campos y botones tienen etiquetas descriptivas.

---

## Requisitos Funcionales

- El formulario de login debe estar centrado vertical y horizontalmente en la pantalla.
- **Ancho del contenedor**: 400px fijo en desktop/tablet; 90% en móvil (< 640px) con padding 1rem en lados.
- **Fondo**: Blanco sólido (`bg-white` en Tailwind); sin gradientes ni patrones.
- **Scope: Frontend-only** - Solo cambios en Razor Pages y Tailwind CSS. Sin modificaciones en Application, Domain, o Infrastructure.
- Debe usarse Tailwind CSS para todos los estilos visuales (colores, tipografía, espaciado, botones, inputs, fondo).
- **Paleta de Marca**: Usar exactamente los mismos colores y estilos del Dashboard (azul primario, grises, Tailwind defaults).
- El diseño debe ser responsivo usando breakpoints Tailwind estándar: mobile (< 640px), tablet (640-1024px), desktop (≥ 1024px).
- **Validación**: Solo al hacer submit (sin validación en tiempo real mientras escribe).
- **Estados visuales completos**: Base, hover, focus, error + transiciones suaves (0.3s) en elementos interactivos + spinner/loading state durante validación.
- Los mensajes de error deben mostrarse de forma visualmente clara y accesible.
- Los botones y campos deben tener outline focus visible y contraste mínimo 4.5:1 (WCAG 2.1 AA).
- **Link Sign Up**: Ubicado debajo del botón "Sign in" como texto pequeño (patrón: "Don't have an account? Sign up").
- **Título de página (ViewData)**: "Sign In - SauronSheet" en la pestaña del navegador.
- El formulario debe ser accesible por teclado, compatible con lectores de pantalla, y cumplir WCAG 2.1 AA.

## Criterios de Éxito

- El login es visualmente atractivo y moderno según estándares actuales.
- El fondo es blanco sólido; el contenedor tiene 400px de ancho (90% en móvil) y está perfectamente centrado.
- El diseño es responsivo y funciona correctamente en mobile (< 640px), tablet (640-1024px), y desktop (≥ 1024px).
- La experiencia visual es consistente con el Dashboard: mismos colores, tipografía, y espaciado.
- **Estados visuales implementados**: Transiciones suaves (0.3s), spinner loading durante POST de login, outline focus visible en inputs y botones.
- El formulario cumple WCAG 2.1 AA: contraste 4.5:1, navegación por teclado funcional, labels descriptivos para lectores de pantalla.
- **Validación**: Se ejecuta solo al hacer submit; no hay validación visual en tiempo real.
- **Link "Sign Up"**: Ubicado debajo del botón con texto pequeño y hover visible (color azul).
- **Título del navegador**: Muestra "Sign In - SauronSheet" en la pestaña.
- Los usuarios reportan una experiencia positiva en pruebas de usabilidad.
- Todos los elementos interactivos tienen hover y focus states visibles.
- Mensajes de error se muestran con color rojo, icono de alerta, y están accesibles para lectores de pantalla.

## Entidades Clave

- Página: /Auth/Login
- Componentes: Formulario de login, campos de email y password, botón de login, mensajes de error

## Supuestos

- Tailwind CSS ya está integrado en el proyecto.
- La paleta de colores y tipografía base ya están definidas en Tailwind config.
- No se requiere rediseñar la lógica de autenticación, solo el aspecto visual.

## Casos Límite

- **Pantallas ultra-pequeñas (< 320px)**: Formulario debe ser legible; padding/fuentes se ajustan.
- **Espacio limitado**: No hay desplazamiento horizontal en ningún breakpoint.
- **Navegadores antiguos (IE 11 no soportado)**: Solo navegadores modernos (Chrome, Firefox, Safari, Edge últimas 2 versiones).
- **Modo oscuro**: No incluido en este feature (deferred a future phase).
- **Estado de error persistente**: Si el login falla, el mensaje permanece hasta que el usuario intente de nuevo.
- **Ambos campos vacíos al cargar**: Mostrar placeholders descriptivos y focus en campo email al cargar.
- **Pega de contraseña**: Debe funcionar correctamente; no debe revelar la contraseña en el campo.
- **Tab order**: Tab debe seguir orden lógico: email → password → login button → "Sign up".

## Especificación de Detalle

### Layout del Formulario

```
┌─────────────────────────────────────────────────────┐
│              [Blanco sólido - bg-white]             │
│                                                       │
│         ┌──────────────────────────────────┐         │
│         │   SauronSheet Logo (opcional)     │         │
│         │                                   │         │
│         │   Sign in to your account         │         │
│         │   (h2 text-3xl font-bold)         │         │
│         │                                   │         │
│         │   Email address                   │         │
│         │   [─────────────────────────]     │         │
│         │   (input, full width)             │         │
│         │                                   │         │
│         │   Password                        │         │
│         │   [─────────────────────────]     │         │
│         │   (input, full width)             │         │
│         │                                   │         │
│         │   [  Sign in (full width)  ]      │         │
│         │   (btn-primary, loading spinner)  │         │
│         │                                   │         │
│         │   Don't have an account?          │         │
│         │   Sign up (link azul)             │         │
│         └──────────────────────────────────┘         │
│                                                       │
└─────────────────────────────────────────────────────┘

Ancho: 400px (desktop/tablet), 90% (móvil)
Fondo: Blanco sólido
Centrado: vertical y horizontal
```

### Paleta de Colores (del Dashboard)

- **Primario**: Azul (usar `bg-blue-600`, `hover:bg-blue-700`, `focus:ring-blue-500`)
- **Error**: Rojo (`bg-red-50`, `text-red-700`)
- **Éxito**: Verde (`bg-green-600`)
- **Texto**: Gris oscuro (`text-gray-900`)
- **Borde**: Gris claro (`border-gray-300`)
- **Focus Ring**: Azul (`focus:ring-2 focus:ring-blue-500`)

### Componentes Específicos

**Título:**
- `ViewData["Title"] = "Sign In"`
- Texto: "Sign in to your account"
- Clase: `text-center text-3xl font-extrabold text-gray-900 mt-6 mb-8`

**Campos de Input:**
- Email: `type="email"`, `required`, placeholder "Enter your email"
- Password: `type="password"`, `required`, placeholder "Enter your password"
- Ambos: `border border-gray-300`, `focus:ring-2 focus:ring-blue-500 focus:border-blue-500`
- Transición: `transition duration-300`

**Botón Submit:**
- Texto: "Sign in"
- Clase: `btn-primary` (o `bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4`)
- Con spinner durante POST (mostrar spinner inline al hacer click)

**Link Sign Up:**
- Texto: "Don't have an account? <a>Sign up</a>"
- Clase: `text-center text-sm text-gray-600`, link `text-blue-600 hover:text-blue-500`
- Ubicación: Debajo del botón con margin-top

**Mensaje de Error:**
- Muestra si hay ErrorMessage
- Clase: `rounded-md bg-red-50 p-4 mb-6`
- Icono + texto rojo
- Accesible: `role="alert"`

- Ninguna dependencia externa crítica. El trabajo es local a la página de login.
- Tailwind CSS ya está integrado en el proyecto (Project Phase 6).

## Clarificaciones

### Session 2026-03-07 (Ronda 1)

- Q: ¿Scope de capas según Constitución? → A: Frontend-only (sin cambios Application/Domain/Infrastructure)
- Q: ¿Paleta de marca para login? → A: Usar exactamente colores del Dashboard (azul, grises, Tailwind defaults)
- Q: ¿Estados visuales y animaciones? → A: Completo (estados base + transiciones 0.3s + spinner loading)
- Q: ¿Estándar de accesibilidad (WCAG)? → A: WCAG 2.1 AA (contraste 4.5:1, outline focus visible, labels descriptivos)
- Q: ¿Breakpoints responsive a probar? → A: Tailwind estándar (mobile, tablet, desktop)

### Session 2026-03-07 (Ronda 2)

- Q: ¿Cuándo debe validarse el formulario? → A: Solo al submit (sin validación en tiempo real)
- Q: ¿Cómo debe ser el fondo? → A: Blanco sólido (minimalista, limpio)
- Q: ¿Ancho máximo del formulario? → A: 400px fijo con padding adaptativo en móvil (90% en mobile)
- Q: ¿Ubicación del link "Sign up"? → A: Debajo del botón como texto pequeño ("Don't have account? Sign up")
- Q: ¿Título de la página (tab)? → A: "Sign In - SauronSheet"

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

### User Story 3 - [Brief Title] (Priority: P3)

[Describe this user journey in plain language]

**Why this priority**: [Explain the value and why it has this priority level]

**Independent Test**: [Describe how this can be tested independently]

**Acceptance Scenarios**:

1. **Given** [initial state], **When** [action], **Then** [expected outcome]

---

[Add more user stories as needed, each with an assigned priority]

### Edge Cases

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right edge cases.
-->

- What happens when [boundary condition]?
- How does system handle [error scenario]?

## Requirements *(mandatory)*

<!--
  ACTION REQUIRED: The content in this section represents placeholders.
  Fill them out with the right functional requirements.
-->

### Functional Requirements

- **FR-001**: System MUST [specific capability, e.g., "allow users to create accounts"]
- **FR-002**: System MUST [specific capability, e.g., "validate email addresses"]  
- **FR-003**: Users MUST be able to [key interaction, e.g., "reset their password"]
- **FR-004**: System MUST [data requirement, e.g., "persist user preferences"]
- **FR-005**: System MUST [behavior, e.g., "log all security events"]

*Example of marking unclear requirements:*

- **FR-006**: System MUST authenticate users via [NEEDS CLARIFICATION: auth method not specified - email/password, SSO, OAuth?]
- **FR-007**: System MUST retain user data for [NEEDS CLARIFICATION: retention period not specified]

### Key Entities *(include if feature involves data)*

- **[Entity 1]**: [What it represents, key attributes without implementation]
- **[Entity 2]**: [What it represents, relationships to other entities]

## Success Criteria *(mandatory)*

<!--
  ACTION REQUIRED: Define measurable success criteria.
  These must be technology-agnostic and measurable.
-->

### Measurable Outcomes

- **SC-001**: [Measurable metric, e.g., "Users can complete account creation in under 2 minutes"]
- **SC-002**: [Measurable metric, e.g., "System handles 1000 concurrent users without degradation"]
- **SC-003**: [User satisfaction metric, e.g., "90% of users successfully complete primary task on first attempt"]
- **SC-004**: [Business metric, e.g., "Reduce support tickets related to [X] by 50%"]
