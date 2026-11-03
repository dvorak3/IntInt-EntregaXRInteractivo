# Práctica XR Interaction Toolkit

## Paquetes y muestras

El `Packages/manifest.json` incluye XR Plug-in Management, OpenXR y XR Interaction Toolkit. Abre el proyecto con Unity 6 y espera a que Package Manager resuelva los paquetes.

En **Window > Package Manager > XR Interaction Toolkit > Samples**, importa:

- **Starter Assets** (incluye el prefab XR Origin con interactores cercanos/lejanos, locomoción y mandos).
- **XR Interaction Simulator** (simulador genérico para Play Mode; se usa como alternativa al simulador externo de Meta).

Ejecuta **XR Practice > Import Samples, Configure OpenXR and Build Scene** para importar las dos muestras, asignar OpenXR a Standalone/Android, activar los perfiles Oculus Touch, Quest Touch Plus y Touch Pro disponibles y generar/guardar la escena. También puedes abrir `Assets/Scenes/SampleScene.unity` y ejecutar por separado **XR Practice > Configure OpenXR for Windows and Quest** y **XR Practice > Build Practice Scene**. La escena incluye plano, cubo y esfera agarrables por ray, una banana de Kenney para interacción directa, una UI de estado y el prefab del simulador.

Los recursos TMP Essentials ya están importados en `Assets/TextMesh Pro/`.

La banana está basada en el modelo **Banana** del [Food Kit de Kenney](https://poly.pizza/bundle/Food-Kit-vOc58LJ0ge), descargado desde Poly Pizza con licencia [CC0](https://creativecommons.org/publicdomain/zero/1.0/). El modelo y el material fuente se incluyen en `Assets/Art/KenneyFoodKit/Banana/`. En la raíz del objeto de escena se añaden `Box Collider`, `Rigidbody`, `XR Grab Interactable` y `GrabStatusUI`; la UI responde al agarre y al soltado.

## OpenXR

En **Edit > Project Settings > XR Plug-in Management**:

1. Instala XR Plug-in Management si Unity solicita completar la instalación.
2. En la pestaña **Standalone**, activa **OpenXR** para usar el simulador en Windows.
3. En **Android**, activa **OpenXR** para Quest.
4. En **OpenXR > Interaction Profiles**, añade los perfiles de Oculus Touch y Meta Quest Touch Plus. Añade Touch Pro para Quest Pro y Eye Gaze solo si se va a utilizar.
5. Revisa las validaciones para el destino correspondiente y aplica **Fix** o **Fix All** cuando Unity lo recomiende. En un Editor Linux, OpenXR Standalone avisa que el runtime de escritorio solo admite Windows x64 y macOS. Conserva OpenXR para Standalone si el proyecto se ejecutará en esos sistemas y valida Android para Quest; no es un error de la configuración Android.

Para Quest, cambia el destino a Android, verifica que el perfil de controlador de Quest corresponda al dispositivo y activa Hand Tracking si la aplicación usará manos.

## Meta XR Simulator

El simulador de Meta es una herramienta externa y no se distribuye con este proyecto. Instala la versión compatible desde la documentación/descargas de Meta y su integración de Unity; después habilita **Meta XR Simulator** y el perfil de interacción Meta correspondiente en OpenXR. Para manos, activa también Hand Tracking. Si Meta Simulator no está disponible, utiliza el prefab genérico **XR Interaction Simulator** que se añade a la escena.

El simulador genérico y el rig deben usarse en Play Mode. Desactiva el objeto **XR Interaction Simulator (solo Play Mode)** antes de compilar para Quest; mantén el XR Origin para ambos modos.

## Prueba

- Con el simulador genérico, usa los controles que aparecen en la interfaz/ayuda del propio simulador.
- Meta XR Simulator: mover la cámara con WASD; rotar con botón derecho + ratón; activar manos izquierda/derecha con 1/2; alternar Ray Interactor con R; agarrar con clic izquierdo y hacer UI click con Espacio. La configuración de la escena no depende de la cámara plana de la plantilla.
- Apunta con el interactor lejano al cubo o la esfera y selecciónalos: cambian de color mientras están seleccionados.
- Acércate a la banana y agárrala con el interactor directo: la UI muestra el estado de agarre y de soltado.
