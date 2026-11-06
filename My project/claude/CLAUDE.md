# Simulación VR de cocina: rondón

Proyecto de Unity **6.3 LTS** para Meta Quest. Usa **Meta XR SDK / Interaction SDK** con Building Blocks (Camera Rig, Hand Grab, Snap, Poke, Ray Canvas). El usuario recorre una cocina y prepara un rondón siguiendo un recetario que se va tachando solo.

- Idioma del proyecto: **español** (nombres de scripts, campos, logs y comentarios).
- XR Operator (MCP `metavr`) está conectado: se puede leer y modificar la escena abierta.
- **Codificación:** varios scripts originales estaban en Windows-1252 (se ven `�` en las tildes). Guardar siempre en **UTF-8**.
- Antes de cambios grandes en la escena, pedir al usuario que haga commit en Unity VCS o duplique la escena.

## Sistema de receta

Patrón general: **algo en la escena detecta una acción → `RecetaEventos.Notificar(id)` → `RecetaManager` marca el paso o sub-paso → `RecetaPasosUI` repinta el libro.**

| Script | Dónde va | Qué hace |
|---|---|---|
| `RecetaEventos` (estático) | En ningún objeto | Canal de eventos. Constantes de Ids y `EnOlla(id)` → `"olla_" + id` |
| `RecetaManager` | Objeto **`Receta manager`** (raíz, **siempre activo**) | Singleton `Instancia`. Pasos con sub-pasos. Lista `Pasos` del Inspector **vacía** → usa `PasosPorDefecto()` del código |
| `RecetaDebugInput` | Mismo objeto | Solo pruebas: **N** avanzar, **B** deshacer, **R** reiniciar, **I** completar todo hasta la estufa (también casilla en el Inspector y menú ⋮) |
| `RecetaPasosUI` | Cada TextMeshPro de pasos del libro | Pinta `[  ]`/`[X]`, tacha, sub-pasos con sangría, progreso `(3/8)`, negrita en el paso actual. Se refresca en `OnEnable` |
| `NavegacionRecetario` | `Recetario` (canvas del libro) | 2 páginas, vuelve a la 1 en `OnEnable`, antirrebote de 0.5 s para el poke |

`ordenEstricto` = **false** a propósito. Con orden estricto, un ingrediente echado "antes de tiempo" entraría en la olla (se destruye) sin contar, y ya no se podría recuperar.

Métodos útiles del manager: `PasosPreviosCompletos(id)`, `TextoPasoActual()`, `CompletarHasta(id)`, `FijarAccionesRequeridas(id, n)`, `CompletarSubpaso(i, j)`.

### Pasos actuales (8)

| # | Paso | Id(s) |
|---|---|---|
| 1 | Llenar la olla de agua y ponerla en el mesón | `olla_con_agua` |
| 2 | Pica y agrega a la olla: Cebolla, Fruta de pan, Yuca, Plátano, Ñame, Ajo, Cola de cerdo, Pescado (sub-pasos) | `olla_cebolla`, `olla_fruta_de_pan`, `olla_yuca`, `olla_platano`, `olla_name`, `olla_ajo_picado`, `olla_cola_de_cerdo`, `olla_pescado` |
| 3 | Agrega enteros: Caracoles, Sal, Tomillo, Pimienta, Chile panameño (sub-pasos) | `olla_caracoles`, `olla_sal`, `olla_tomillo`, `olla_pimienta`, `olla_chile` |
| 4 | Echar la leche de coco | `olla_leche_coco` |
| 5 | Llevar la olla a la estufa y encenderla | `estufa_encendida` |
| 6 | Llevar el plato a la mesa | `plato_en_mesa` |
| 7 | Volver a la estufa y llenar el cucharón | `cucharon_lleno` |
| 8 | Volver a la mesa a servir | `cucharon_servido` |

Reparto en el libro (`RecetaPasosUI` → Primer Paso / Cantidad): pasos 1 / 2 / 3 / 4 a 8 → (1,1), (2,1), (3,1), (4,6; la cantidad se recorta al total de pasos). Los títulos de columna son objetos `Titulo` con texto fijo.

## Ingredientes y olla

| Script | Dónde va | Qué hace |
|---|---|---|
| `IngredientePicable` | Raíz de cada ingrediente que se pica | `Id Ingrediente`, modelos entero/picado, `cortesNecesarios`. `IncorporarAlaOlla()` devuelve bool y **destruye** el objeto |
| `IngredienteEntero` | Raíz de cada ingrediente entero (también la leche de coco) | `Id Ingrediente`. `IncorporarAlaOlla()` devuelve bool y destruye el objeto |
| `CuchilloPicador` | Raíz del cuchillo | Trigger de la hoja → `Picar()`. Solo pica si está agarrado. Vuelve a su sitio al soltarlo |
| `DetectorOlla` | `OLLA_COLOR (1)/DetectorIngredientes` (trigger dentro de la olla) | Acepta ingredientes listos (picados o enteros) **solo si la olla tiene agua**. Cada uno cuenta una vez. **Es el único que notifica** (`EnOlla(id)`); los scripts de ingrediente no notifican |
| `OllaEstado` | Raíz `[BuildingBlock] Olla` | Estados `Vacia` / `ConAgua` / `Rondon`. Alterna solo los **Renderers** de `Visual Vacia` (Cylinder, Torus, Torus.001) y enciende o apaga los objetos `OLLAagua` y `rondon en olla`, cuyos colliders apaga en `Awake` |
| `ZonaReceta` | Detectores trigger de las snap zones | Detecta `OllaEstado` dentro. Acciones: `LlenarOlla` (lavamanos), `NotificarPaso` (mesón, `olla_con_agua`, requiere agua), `Ninguna` (estufa: solo expone `OllaDentro`). `Solo Si Esta Suelta` debe estar **desmarcado** |
| `VolverAPosicion` | Objetos agarrables (libro de la barra, raíz de `Cucharón [BuildingBlock]`) | Vuelven a su posición inicial al soltarlos |

Todos los ingredientes con varias unidades (3 cebollas, 5 ajos, 2 plátanos) llevan el mismo Id. Todos los ajos son picables (`ajo_picado`).

## Estufa, cucharón y plato

- **`ControlFogon`** (en la perilla del fogón usado, con `Zona Estufa` = `SnapDetectionOlla (2)`): solo enciende si hay olla encima **y** `PasosPreviosCompletos(estufa_encendida)`. Notifica `estufa_encendida`. Tras `Segundos Coccion` llama a `olla.HacerRondon()`.
  - El `Interactable Unity Event Wrapper` de la perilla debe llamar a **`ControlFogon.AlternarFogon()`** en `When Select`, **nunca** a `GameObject.SetActive` del fuego directamente: eso se saltaría la comprobación.
- **`Cucharon`** (`Cucharón [BuildingBlock]/CucharonMaster`): se llena al tocar una olla con `OllaEstado.EstadoActual == Rondon` (ya no busca por nombre, porque los colliders del rondón están apagados). Notifica `cucharon_lleno` solo si los pasos anteriores están completos.
- **`Plato`** (`Plato[BuildingBlock]/rondon en plato`): al tocarlo el cucharón lleno, vacía el cucharón, muestra la comida y notifica `cucharon_servido` si los pasos previos están completos. Si no lo están, no cuenta, pero se puede volver a llenar el cucharón y servir otra vez.

- **`SnapNotificarPaso`** (`SnapZonePlato`, junto al `SnapInteractable`, que usa el Rigidbody de `SnapDetectionPlato`): cuando el plato encaja al soltarlo, notifica `plato_en_mesa`. No exige que esté servido ni que los pasos anteriores estén completos.

- **`InspeccionPlato`** (raíz de `Plato[BuildingBlock]`, con `Camara` = `CenterEyeAnchor`): con la receta completa apaga el `SnapInteractor` y se pone como transformador del `Grabbable`. Al agarrar, el plato flota frente al usuario y gira sobre su centro visual siguiendo la mano. Al soltarlo, espera `Segundos Antes De Volver` y regresa a su sitio. Antes de completar la receta, el agarre es normal.

## Recetario e interfaz VR

- **`ControlRecetario`** (en `Receta manager`): al agarrar el libro de la barra (`Grabbable`) activa `AnclaLibro` y oculta `ZonasPaneles`. Mientras está abierto y el libro ya se soltó, apaga `Desactivar Mientras Abierto` (agarre a distancia y `HandGrabInteractable` del libro) para que no compitan con el rayo o el poke. `Cerrar()` lo revierte.
- **`AnclaLibro`**: fuera de `CenterEyeAnchor`, en un sitio fijo sobre la barra, con **`MirarAlUsuario`**. Gira para mirar al usuario (solo eje Y), pero **se congela** mientras hay hover o select de cualquier `IPointable` hijo o una mano cerca.
- **Pasar página:** Building Block `ISDK_PokeInteraction` + `Interactable Unity Event Wrapper` → `When Select` → `NavegacionRecetario.SiguientePagina`.
- **`PanelSeguirCamara`**: en cada zona de `ZonasPaneles` (ZonaBarra, ZonaEstufa, ZonaAlacena, Comienzo, MenuZonas, ZonaNevera). Seguimiento perezoso, se congela al apuntar y apaga el agarre a distancia mientras se apunta (`Desactivar Mientras Apunta`). **Nunca** un objeto con este script dentro de otro que también lo tenga.

## Audio

- Los clips están en `Assets/JakelineVR/AUDIOS/OneDrive_2026-11-06/realidad virtual/`.
- **`Ambiente`** (raíz de la escena): hijos `Mar`, `Gaviotas`, `Musica` y `Palmeras`, cada uno con un `AudioSource` 2D en bucle y `Play On Awake`. Se mezclan cambiando el `Volume` de cada uno. `mar` y `musica caribeña` usan Load Type = Streaming para ahorrar memoria.

- **`SonidosCocina`** en **`Efectos`** (raíz de la escena): un `AudioSource` hijo por sonido, sin `Play On Awake`. Los scripts avisan con métodos estáticos y el sonido se coloca en el punto de la acción (3D); `Celebracion` es 2D.

| Sonido | Cuándo | Quién avisa |
|---|---|---|
| `Cortar` | Cada corte válido | `IngredientePicable.Picar` |
| `PrenderEstufa` | Solo si el fogón enciende de verdad | `ControlFogon.IntentarEncender` |
| `AguaHirviendo` (bucle) | Hay fuego, olla encima y aún no es rondón | `ControlFogon.Update` |
| `SopaEnPlato` | El cucharón lleno toca el plato | `Plato.OnTriggerEnter` |
| `PasarPagina` | Al pasar página, después del antirrebote | `NavegacionRecetario.SiguientePagina` |
| `Celebracion` | Al completar la receta | `onRecetaCompletada` |

## Lecciones aprendidas (evitar repetir)

- **Rayo o láser que parpadea:** casi siempre es (a) la superficie moviéndose mientras se apunta, (b) dos scripts moviendo la misma jerarquía o (c) otro interactor (agarre a distancia o cercano) compitiendo en el mismo grupo de la mano. La solución es congelar al interactuar y apagar los interactores que compiten.
- **Aspectos visuales dentro de objetos agarrables:** sin Rigidbody ni Grabbable propios, sin colliders activos, y asignados **desde la jerarquía**, no desde la carpeta Project.
- **Cambiar valores por defecto en código no modifica los componentes ya puestos:** hay que cambiarlos en el Inspector.
- **Snap del Interaction SDK:** el `SnapInteractor` comprueba si **su punto** (`Snap Pose Transform` o, si está vacío, el pivote) está dentro de los colliders del `Rigidbody` del `SnapInteractable`. Pivote desplazado = no encaja.

## Pendiente

1. **Otras perillas de la estufa** (1, 2, 3): sus wrappers pueden encender el fuego directamente sin comprobaciones. Decidir si se desactivan.
2. **Ventana AI Tools de Unity:** muestra "Claude CLI not found" porque npm instala `claude.cmd`. Es solo un aviso de esa pantalla: `claude mcp list` muestra `metavr` conectado.
