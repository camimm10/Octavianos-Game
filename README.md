# Los Octavianos 🐙 ● ■ ▲

**Un juego de PC y una app de móvil para cuidar la mente y el cuerpo, de la mano de Octavio.**

Proyecto de **Lenguajes y Paradigmas** (UCJC, curso 2026-2027), dentro del universo de [**Destello**](https://github.com/camimm10/LYP-2026), nuestro lenguaje de programación esotérico de luces de colores.

- Enzo Sánchez Pérez
- Camila Montoya Morales
- Paloma Domínguez Montero
- Diego Andrés Boscán Guillamón

⬇️ **[Descargar el juego (PC y Android)](https://github.com/camimm10/Octavianos-Game/releases/latest)** · 📊 [Presentación del proyecto](Documentacion/LosOctavianos.pptx)

---

## Qué es

Los Octavianos tiene dos partes que se conectan entre sí:

| | **Juego de PC** | **App de móvil** |
|---|---|---|
| Qué haces | Vives la historia de Octavio en 5 etapas y superas pruebas | Cuidas tus hábitos: actividades, misiones fuera de casa y un diario de cómo te sientes |
| Qué ganas | Algas doradas para el **Armario de Octavio** | Complementos para Octavio y algas doradas |
| Cómo se unen | Escribes en el PC los **códigos de linternas Destello** que te da el móvil | Lo que haces en tu vida real se nota en el juego |

La idea es sencilla: **lo que haces para cuidarte en la vida real hace crecer a Octavio en el juego.**

---

## Nuestros valores

- **Salud mental y física.** Es el objetivo de todo el proyecto: mejorar el bienestar de las personas con una experiencia **accesible, fácil y divertida**. La app propone moverse, leer, respirar, crear y hablar con otros, y pregunta cómo te sientes antes y después de cada actividad.
- **Para todas las edades.** Controles simples, textos claros, instrucciones antes de cada minijuego y tres niveles de dificultad. Los colores siempre van con una forma (● rojo, ■ verde, ▲ azul), como en UNO ColorADD, para que también lo puedan jugar personas con daltonismo.
- **Conciencia ambiental.** Hay misiones para limpiar espacios naturales, y el propio proyecto es **sencillo a propósito**: gráficos hechos por código, sin modelos pesados ni servidores. Usar menos código y menos cómputo, también con la IA, gasta menos energía.
- **Hábitos saludables que se juegan.** No es una app de deberes: es un juego dinámico, con recompensas y un personaje que te acompaña.
- **Uso ético y responsable de la IA.** La usamos como ayuda para programar, con moderación, y el equipo revisa y decide cada cosa. Además:
  - Ningún dato sale del móvil: el diario, las fotos y los pasos se guardan solo en tu teléfono.
  - La app **no sustituye a un profesional** de la salud.

---

## 🎮 El juego de PC

### Cómo se juega
- **Nadar:** W A S D o flechas · **Hablar:** E · **Hablar con luz:** 1 ● · 2 ■ · 3 ▲ · **Pausa:** Esc
- **Cámara:** Q / E o clic derecho + ratón para girar · rueda para acercar · **R** para mirar hacia el haz de luz. Arriba a la derecha, una **brújula** señala el haz de luz más cercano y a cuántos metros está.
- En cada etapa llegas a una **prueba** y eliges tu camino: **MENTE** (lógica y lenguaje), **TENTÁCULOS** (habilidad) o **CORAZÓN** (emociones y empatía). Si fallas, puedes probar otro.
- Cada minijuego empieza con una **pantalla de descripción e instrucciones**. Ahí eliges la **dificultad** (Fácil, Normal o Difícil, con Ctrl+F1, Ctrl+F2, Ctrl+F3 o clic). Las pruebas de programar en Destello no tienen niveles: traen su propia chuleta (tecla H).
- En **Difícil** puedes comprar una **pista** con algas doradas. La pista se queda a la vista durante toda la prueba.
- Al superar una prueba ganas **algas doradas**: +2 en Fácil, +3 en Normal y +5 en Difícil.
- Las zonas tienen límites: Octavio nunca se pierde en la oscuridad.

### Las 5 etapas

| Etapa | Historia | 🧠 Mente | 🐙 Tentáculos | ❤️ Corazón |
|---|---|---|---|---|
| **1 · El humano que cayó del cielo** | Un humano cae al mar. Brisa huye, pero Octavio decide salvarlo. | *Las corrientes*: gira piezas para unir la corriente (puzle) | *El arrastre*: sube y esquiva rocas con el humano a cuestas (reflejos) | *Destellos de ayuda*: repite secuencias de luz (memoria) |
| **2 · El géiser Destello** | Octavio salta al géiser prohibido. La erupción los lanza por los aires y los fusiona. | *La presión del géiser*: abre todas las válvulas (puzle lógico) | *La caída*: esquiva gaviotas y rocas en plena caída (reflejos) | *Encontrar el valor*: ordena los recuerdos de Octavio (memoria) |
| **3 · Algo ha cambiado** | Octavio vuelve a casa más listo que nunca. Hay que abrir la despensa antes de la marea fría. | *El lenguaje de la luz*: programa la contraseña en Destello (programación) | *La gran recolección*: recoge algas y esquiva cangrejos (habilidad) | *¿Qué te pasa, Octavio?*: recupera la confianza de tu familia (diálogo) |
| **4 · El nacimiento de Octópolis** | Años después, la colonia ya no cabe en las rocas y nace la primera ciudad. | *La escuela de la luz*: lee destellos y enseña a los pequeños (base 3) | *Levantar Octópolis*: apila bloques con precisión (timing) | *Un sitio para cada uno*: escucha a cada vecino y dale su lugar (empatía) |
| **5 · La gran traición** | Las orcas piden ayuda… y es una trampa. Hay que esconder a los pequeños. | *El mensaje de luz*: un mensaje que las orcas no entienden, con un bucle MIENTRAS (programación) | *El gran escondite*: lleva a los pequeños a la cueva esquivando orcas | *Es solo un juego*: calma a los pequeños convirtiendo el miedo en un juego (diálogo) |

Las pruebas de **Mente** usan el lenguaje **Destello** de verdad: rojo ● = 0, verde ■ = 1 y azul ▲ = 2, en base 3, con una pila e instrucciones como IMPRIMIR, PALABRA o MIENTRAS.

### El Armario de Octavio
Con las algas doradas personalizas a Octavio: **color** de piel, **dibujos** (lunares, rayas, estrellas, corazones, manchas), **brillo**, **forma**, **tamaño**, **ojos** (tamaño, forma, color del iris, pestañas), **gorros** (copa, lana, gorra, lazo, corona de flores), **ropa** (tutú, vestido, bufanda, perlas) y **cara** (gafas, mejillas, bigote). Octavio aparece en 3D girando, y lo que compras lo lleva en todas las etapas. En la pestaña **Del móvil** le pones o le guardas los complementos que dibujaste en la app (corona, pajarita…). Abajo a la derecha sale un **código de 7 letras**: escríbelo en el móvil («Octavio como en el PC») y en la cámara RA saldrá igual que en el PC.

---

## 📱 La app de móvil

Tu rincón de bienestar, con Octavio como compañero:

- **¿Cómo te encuentras?** Un diario con 5 caras de ánimo, emociones y unas líneas. Se pregunta **antes y después** de cada actividad para ver qué te ayuda.
- **Actividades**, una por camino del juego:
  - **Leer**: título y fotos de la página donde empiezas y donde terminas.
  - **Caminar**: cuenta tus pasos y la distancia con el GPS.
  - **Respirar**, **meditar**, **idiomas**, **tocar un instrumento**, **cocinar** y **hablar con alguien**.
  - **Dibujar**: copias un complemento en papel, le haces una foto y la app compara tu dibujo con la plantilla. Si se parece, te da un **código Destello** para que Octavio lo lleve en el PC.
- **Misiones fuera de casa:**
  - **Recoger basura**, con foto del antes y del después. Se comprueba con el GPS y el tiempo.
  - **Biblioteca o librería** y **museo**.
- **Mis colecciones**: libros, cuadros, campos limpios, dibujos y objetos.
- **Mis estadísticas**: evolución del ánimo, qué actividades te ayudan más y pasos por día.
- **Octavio en tu mundo**: realidad aumentada. Funciona con ARCore o, si el móvil no lo tiene, en modo cámara.
- **Bolsillo**: tus algas doradas. Con 5 algas generas un **código de 7 linternas** que pasa esas algas al juego de PC.

---

## 🔗 Cómo se conectan el móvil y el PC

Sin internet ni cuentas: los dos se hablan **en Destello**.

1. En el móvil consigues un código de linternas, por ejemplo `▲ ● ■ ■`.
2. En el PC vas a **Menú principal → «Objetos del móvil»** y lo escribes con las teclas 1 ●, 2 ■ y 3 ▲.
3. Un código de **4 linternas** desbloquea un complemento. Uno de **7 linternas** (empieza por ● ●) suma **5 algas doradas**. Cada código de algas solo vale una vez.

---

## Probarlo

Las descargas están en **[Releases](https://github.com/camimm10/Octavianos-Game/releases/latest)**.

- **PC (Windows):** descarga `LosOctavianos-Windows.zip`, descomprímelo y abre `LosOctavianos/LosOctavianos.exe`. Si Windows avisa, pulsa «Más información» y luego «Ejecutar de todas formas».
- **Móvil (Android):** descarga `LosOctavianos.apk` en el móvil, instálalo y acepta los permisos de cámara, ubicación y actividad física.
- **Abrir el proyecto:** Unity **6.3 LTS (6000.3.29f1)**, con URP. Abre la carpeta `OctavianosGame` desde Unity Hub.
  - Para compilar: menú **Octavianos → Construir exe de Windows** o **Construir APK de Android**.

### Estructura
```
OctavianosGame/Assets/Scripts/
├─ Etapas/       las 5 etapas y su historia
├─ Minijuegos/   los 15 minijuegos (base común con dificultad y pistas)
├─ Destello/     intérprete del lenguaje Destello
├─ Octavio/      Octavio de dibujos animados, colores, límites de zona
├─ Juego/        menú, armario, algas, complementos
├─ Movil/        app de bienestar, diario, comparador de dibujos
├─ RA/           realidad aumentada
└─ Ambiente/     decorado del fondo del mar, hecho por código
Documentacion/   presentación del proyecto
```

---

## Créditos

- Personaje humano: **Mini Characters** de [Kenney](https://kenney.nl) (CC0).
- Tipografía: **Fredoka** (SIL Open Font License).
- Todo lo demás (Octavio, decorados y sonidos) está hecho por código.
- Parte del código se desarrolló con ayuda de Claude (IA), siempre revisado por el equipo.
