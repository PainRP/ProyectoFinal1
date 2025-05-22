# El proyecto final 1 - "Docify"

El proyecto "Docify" lo desarrolle en C# mediante WinForms. Su propósito es la 
generación de investigaciones académicas concisas a partir de un tema proporcionado por 
el usuario, permitiendo posteriormente la exportación de los resultados obtenidos a 
formatos de documento estandarizados como Microsoft Word y PowerPoint, además de 
asegurar la persistencia de la información en una base de datos.

## Diseño de la Interfaz de Usuario (UI)

La interfaz la cree para ser responsive mediante la utilización de controles WinForms como
“Panels” y “TableLayoutPanels”. Se ha implementado extensivamente el uso de 
propiedades como Dock = Fill con el fin de asegurar que los elementos de la UI se ajusten 
dinámicamente al tamaño de la ventana, lo cual proporciona una experiencia de usuario 
consistente en diversas resoluciones de pantalla.

Adicionalmente, se han incorporado efectos visuales sutiles para optimizar la 
interactividad: los botones modifican su color de fondo y de texto al detectar el cursor del 
ratón sobre ellos (MouseEnter y MouseLeave), ofreciendo una retroalimentación visual 
clara al usuario.

## Flujo de Trabajo del Programa

El programa opera siguiendo un flujo de trabajo secuencial y simplificado:

1. **Entrada de Tema**: El usuario introduce el tema de investigación en un TextBox
   designado (`txtInput`).
2. **Generación de Contenido**: Tras la activación del botón "Enviar" (`btnEnviar`), la 
   aplicación establece comunicación con la API de Google Gemini para generar una 
   investigación académica breve basada en el tema suministrado. Durante este 
   proceso, el botón "Enviar" se desactiva para prevenir envíos redundantes, y se 
   visualiza un panel de resultados.
3. **Visualización y Edición**: La respuesta generada por la inteligencia artificial se 
   presenta en un RichTextBox (`rtbOutput`) ubicado en el panel de resultados. Esta 
   característica confiere al usuario la capacidad de revisar el contenido y efectuar 
   modificaciones previas a su procesamiento final.
4. **Acciones Post-Generación**: El usuario dispone de dos alternativas tras la 
   generación del contenido:
   - **Aceptar (`btnAceptar`)**: En caso de que el contenido sea satisfactorio, la 
     selección de "Aceptar" conlleva el almacenamiento de la investigación en la 
     base de datos, así como la generación de un documento de Word (.docx) y 
     una presentación de PowerPoint (.pptx) que incorporan el contenido. 
     Posteriormente, la interfaz retorna a su estado inicial, preparada para una 
     nueva investigación.
   - **Rechazar (`btnRechazar`)**: Si el contenido no cumple con los requisitos 
     esperados, la elección de "Rechazar" simplemente limpia los campos de 
     entrada y salida, restaurando la aplicación a su estado inicial sin proceder al 
     guardado o la generación de documentos.

## Gestión de Archivos y Directorios

El programa gestiona la creación de una carpeta específica, llamada `docify`, dentro de la 
carpeta "Mis Documentos" del usuario. Todos los archivos de Word y PowerPoint 
generados se almacenan en esta ubicación, y tiene una lógica en la creación de nombre 
única para evitar la sobrescritura (por ejemplo, `Investigacion1.docx`, 
`Investigacion2.docx`).

## APIs y Librerías Utilizadas

Para el desarrollo de "Docify", se han integrado diversas APIs y librerías fundamentales 
que posibilitan la funcionalidad principal del programa:

1. **API de Google Gemini** (a través de la librería `GenerativeAI` para C#)
   - **Propósito**: Esta constituye la API central para la generación de contenido. Permite 
     a la aplicación transmitir una instrucción ("prompt") a un LLM de Google 
     (específicamente `gemini-1.5-flash`) y recibir una respuesta textual detallada.
   - **Implementación**: La clave de la API se recupera de una variable de entorno 
     (`GOOGLE_API_KEY`), lo cual asegura la seguridad y flexibilidad en la 
     configuración. La clase `GoogleAi` es responsable de la comunicación con el servicio 
     de Gemini. La instrucción se estructura meticulosamente para solicitar una 
     investigación académica con un formato predefinido (título, párrafos).

2. **Open XML SDK** (`DocumentFormat.OpenXml`)
   - **Propósito**: Esta librería es esencial para la manipulación programática de 
     documentos de Microsoft Office (Word, PowerPoint, Excel) sin requerir la 
     instalación de Office. Facilita la creación, lectura y modificación de archivos en 
     formatos Open XML (como `.docx` y `.pptx`).
   - **Implementación**:
     - **Para Word (.docx)**: Se emplea `WordprocessingDocument.Create` para 
       generar un nuevo documento. El contenido del `RichTextBox` se segmenta en 
       líneas; la primera línea se formatea como título (en negrita y centrado), y las 
       subsiguientes como párrafos estándar.
     - **Para PowerPoint (.pptx)**: Se duplica una plantilla preexistente 
       (`PlantillaPowerPoint.pptx`) con el fin de preservar un diseño preestablecido. 
       Posteriormente, se itera sobre los párrafos de la investigación generada, 
       creando una nueva diapositiva para cada uno. Se busca un diseño de 
       diapositiva "en blanco" o `blank` dentro de la plantilla para la inserción del 
       contenido en un cuadro de texto. La construcción de la estructura de las 
       diapositivas y la inserción de texto se efectúa mediante objetos del SDK, 
       tales como `SlidePart`, `Shape`, `TextBody`, `Paragraph`, y `Run`.

3. **Microsoft.Data.SqlClient**
   - **Propósito**: Esta es la librería oficial de Microsoft para la conectividad con bases de 
     datos SQL Server desde aplicaciones .NET.
   - **Implementación**: Se utiliza para establecer y gestionar la conexión con la base de 
     datos a través de la clase `DataBaseManager` (una utilidad personalizada). La función 
     `GuardarBD` construye una consulta SQL (`INSERT`) parametrizada para insertar el 
     tema de la investigación (Prompt) y el resultado generado (Resultado) en la tabla 
     `Investigaciones`. Se incorpora manejo de excepciones para la conexión y las 
     operaciones de almacenamiento.

## Base de Datos

El proyecto "Docify" incorpora una base de datos con el fin de mantener un registro de las 
investigaciones generadas.

La base de datos, es simple, es una simple tabla con lo necesario, un id, el prompt, la 
respuesta de la ia, y la fecha de guardado. Además utilizo el uso de clase, para realizar 
acciones como el de comprobar que se conecte a la base de datos.

## Desafíos y Soluciones

Uno de los desafíos más significativos mientras lo desarrallaba fue la generación de 
documentos de PowerPoint. problemas relacionados con la corrupción de archivos. La 
solución de este problema me implicó un analizar la especificación Open XML y la 
implementación meticulosa de los procesos de creación de diapositivas, garantizando que 
cada componente (como `CommonSlideData`, `ShapeTree`, `NonVisualShapeProperties`, 
`TextBody`) se incorporara en el orden y la jerarquía apropiados para preservar la integridad 
del archivo. El utilizar una plantilla para realizar la presentación ayudo demasiado a realizar 
esta parte del programa.

Utilizo Open XML SDK en lugar de Office Interop debido a que tengo problemas al 
utilizar office interop ya que no reconoce mi versión de office, entonces preferí el utilizar 
open xml sdk, que permitiría que incluso las personas sin office lo puedan usar.
