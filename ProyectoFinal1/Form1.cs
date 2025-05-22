using Microsoft.Data.SqlClient;
using ProyectoFinal1.Utils;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using GenerativeAI;
using DocumentFormat.OpenXml.Wordprocessing;
using Control = System.Windows.Forms.Control;



namespace ProyectoFinal1
{
    public partial class Form1 : Form
    {
        private DataBaseManager _dbManager;
        string templateFileName = "PlantillaWord.dotx";
        string powerPointTemplateFileName = "PlantillaPowerPoint.pptx";
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private string googleAPIKey;
        private GoogleAi googleAI;


        public Form1()
        {
            InitializeComponent();
            AsignarEventosHover(this.Controls);

            googleAPIKey = System.Environment.GetEnvironmentVariable("GOOGLE_API_KEY");

            if (string.IsNullOrEmpty(googleAPIKey))
            {
                MessageBox.Show("Error: La variable de entorno GOOGLE_API_KEY no est  configurada.", "Error de configuraci n", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
            else
            {
                googleAI = new GoogleAi(googleAPIKey);
            }

        }
        private void Form1_Load(object sender, EventArgs e)
        {
            _dbManager = new DataBaseManager();

            if (!_dbManager.TestConnection())
            {
                MessageBox.Show("No se pudo conectar a la base de datos. Verifique la conexión e intente nuevamente.", "Error de Conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }
            MessageBox.Show("Conexión a la base de datos establecida correctamente.", "Conexión Exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);



            panelInput.Dock = DockStyle.Fill;
            panelOutput.Visible = false;

        }
        private async void AsignarEventosHover(Control.ControlCollection controls)
        {
            foreach (Control control in controls)
            {
                if (control is Button btn)
                {
                    btn.MouseEnter += (s, e) => btn.BackColor = System.Drawing.Color.FromArgb(124, 111, 100);
                    btn.MouseEnter += (s, e) => btn.ForeColor = System.Drawing.Color.FromArgb(242, 229, 188);
                    btn.MouseLeave += (s, e) => btn.BackColor = System.Drawing.Color.FromArgb(249, 245, 215);
                    btn.MouseLeave += (s, e) => btn.ForeColor = System.Drawing.Color.FromArgb(0,0, 0);
                }

                if (control.HasChildren)
                    AsignarEventosHover(control.Controls);
            }
        }
        private async void btnEnviar_Click(object sender, EventArgs e)
        {
            btnEnviar.Enabled = false;
            var model = googleAI.CreateGenerativeModel("models/gemini-1.5-flash");
            var response = await model.GenerateContentAsync(
                $@"
                 Actúa como un investigador académico y genera una breve investigación sobre el tema que te proporciono.
                - La primera línea será el título.
                - Entre 3 y 6 párrafos de texto corrido.
                - No coloques símbolos especiales ni espacios invisibles.
                - Quedate centrado al tema (lo que pide).
                Tema: {txtInput.Text}
                ");
            rtbOutput.Text = response.Text;
            await Task.Delay(1000);

            panelInput.Visible = false;
            panelOutput.Dock = DockStyle.Fill;
            panelOutput.Visible = true;
        }

        private async void btnAceptar_Click(object sender, EventArgs e)
        {
            btnEnviar.Enabled = true;
            panelOutput.Visible = false;
            panelInput.Visible = true;
            string fullTemplatePath = Path.Combine(baseDirectory, templateFileName);

            string texto = rtbOutput.Text;

           GuardarBD();
           GuardarWord();
           GuardarPowerPoint(texto);
           txtInput.Clear();
           rtbOutput.Clear();
        }

        private async void btnRechazar_Click(object sender, EventArgs e)
        {
            btnEnviar.Enabled = true;
            panelOutput.Visible = false;
            panelInput.Visible = true;
            txtInput.Clear();
            rtbOutput.Clear();
        }

        public void GuardarBD()
        {
            string sql = @"
                INSERT INTO Investigaciones (Prompt, Resultado)
                VALUES (@PromptValue, @ResultadoValue);
                SELECT SCOPE_IDENTITY();";

            using (var connection = _dbManager.GetConnection())
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PromptValue", txtInput.Text);
                    command.Parameters.AddWithValue("@ResultadoValue", rtbOutput.Text);

                    try
                    {
                        connection.Open();
                        object result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            int newInvestigacionId = Convert.ToInt32(result);
                            Console.WriteLine($"Registro insertado con ID: {newInvestigacionId}");
                        }
                        else
                        {
                            Console.WriteLine("Registro insertado, pero no se pudo recuperar el ID.");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar la investigación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }


                }
            }
        }

        private async void GuardarWord()
        {
            string misDocumentosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string docifyFolderName = "docify";
            string docifyPath = Path.Combine(misDocumentosPath, docifyFolderName);

            if (!Directory.Exists(docifyPath))
            {
                try
                {
                    Directory.CreateDirectory(docifyPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al crear la carpeta 'docify': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            string baseFileName = "Investigacion";
            string extension = ".docx";
            int contador = 1;
            string filePath;

            do
            {
                filePath = Path.Combine(docifyPath, $"{baseFileName}{contador}{extension}");
                contador++;
            } while (File.Exists(filePath));

            try 
            {
                using (WordprocessingDocument wordDoc = WordprocessingDocument.Create(filePath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
                {
                    MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(); 
                    Body body = mainPart.Document.AppendChild(new Body());

                    var lines = rtbOutput.Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        var paragraph = new Paragraph();
                        var run = new Run(new DocumentFormat.OpenXml.Wordprocessing.Text(lines[i]));

                        if (i == 0)
                        {
                            RunProperties runProps = new RunProperties(new Bold());
                            run.PrependChild(runProps);

                            ParagraphProperties paraProps = new ParagraphProperties(
                                new Justification() { Val = JustificationValues.Center }
                            );
                            paragraph.PrependChild(paraProps);
                        }

                        paragraph.Append(run);
                        body.Append(paragraph);
                    }
                }
                MessageBox.Show($"Contenido exportado a Word en:\n{filePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar el documento Word: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void GuardarPowerPoint(string contenido)
        {
            string misDocumentosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string docifyFolderName = "docify";
            string docifyPath = Path.Combine(misDocumentosPath, docifyFolderName);

            if (!Directory.Exists(docifyPath))
            {
                try
                {
                    Directory.CreateDirectory(docifyPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al crear la carpeta 'docify': {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            string baseFileName = "Presentacion_Investigacion";
            string extension = ".pptx";
            int contador = 1;
            string outputFilePath;

            string templatePath = Path.Combine(baseDirectory, powerPointTemplateFileName);

            if (!File.Exists(templatePath))
            {
                MessageBox.Show("La plantilla de PowerPoint no se encontró.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            do
            {
                outputFilePath = Path.Combine(docifyPath, $"{baseFileName}{contador}{extension}");
                contador++;
            } while (File.Exists(outputFilePath));


            try
            {
                File.Copy(templatePath, outputFilePath);

                using (PresentationDocument presentation = PresentationDocument.Open(outputFilePath, true))
                {
                    PresentationPart presentationPart = presentation.PresentationPart;

                    if (presentationPart == null)
                    {
                        MessageBox.Show("La plantilla no contiene una parte de presentación válida.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    SlideLayoutPart layoutPart = presentationPart.SlideMasterParts
                        .SelectMany(smp => smp.SlideLayoutParts)
                        .FirstOrDefault(lp =>
                            lp.SlideLayout.CommonSlideData?.Name?.Value?.ToLower().Contains("en blanco") == true ||
                            lp.SlideLayout.CommonSlideData?.Name?.Value?.ToLower().Contains("blank") == true);

                    if (layoutPart == null)
                    {
                        layoutPart = presentationPart.SlideMasterParts.FirstOrDefault()?.SlideLayoutParts.FirstOrDefault();
                    }

                    if (layoutPart == null)
                    {
                        MessageBox.Show("No se encontró un diseño de diapositiva válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    string[] parrafos = contenido.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    uint nextSlideId = 256;
                    if (presentationPart.Presentation.SlideIdList == null)
                        presentationPart.Presentation.SlideIdList = new SlideIdList();

                    SlideIdList slideIdList = presentationPart.Presentation.SlideIdList;

                    foreach (string parrafo in parrafos)
                    {
                        SlidePart slidePart = presentationPart.AddNewPart<SlidePart>();

                        Slide slide = new Slide(
                            new CommonSlideData(
                                new ShapeTree(
                                    new NonVisualGroupShapeProperties(
                                        new NonVisualDrawingProperties() { Id = 1U, Name = "" },
                                        new NonVisualGroupShapeDrawingProperties(),
                                        new ApplicationNonVisualDrawingProperties()
                                    ),
                                    new GroupShapeProperties()
                                )
                            ),
                            new ColorMapOverride(new A.MasterColorMapping())
                        );

                        slidePart.Slide = slide;

                        slidePart.AddPart(layoutPart);

                        var shapeTree = slide.CommonSlideData.ShapeTree;
                        var textShape = CrearTextBox(parrafo);
                        shapeTree.Append(textShape);

                        slide.Save();

                        SlideId slideId = new SlideId()
                        {
                            Id = nextSlideId++,
                            RelationshipId = presentationPart.GetIdOfPart(slidePart)
                        };

                        slideIdList.Append(slideId);
                    }

                    presentationPart.Presentation.Save();
                }

                MessageBox.Show($"Se generaron {contenido.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length} diapositivas:\n{outputFilePath}", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear presentación: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private  DocumentFormat.OpenXml.Presentation.Shape CrearTextBox(string contenido)
        {
            var shape = new DocumentFormat.OpenXml.Presentation.Shape(
                new NonVisualShapeProperties(
                    new NonVisualDrawingProperties() { Id = 2U, Name = "CuadroTextoContenido" },
                    new NonVisualShapeDrawingProperties(new A.ShapeLocks() { NoGrouping = true }),
                    new ApplicationNonVisualDrawingProperties()),

                new ShapeProperties(
                    new A.Transform2D(
                        new A.Offset() { X = 914400, Y = 914400 },
                        new A.Extents() { Cx = 6858000, Cy = 4000000 })),

                new TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle()
                )
            );

            var textBody = shape.TextBody;
            foreach (var paragraph in CrearParrafo(contenido))
            {
                textBody.Append(paragraph);
            }

            return shape;
        }

        private  IEnumerable<A.Paragraph> CrearParrafo(string textContent)
        {
            var paragraphs = new List<A.Paragraph>();

            foreach (var line in textContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var paragraph = new A.Paragraph();

                var run = new A.Run();
                run.Append(new A.RunProperties() { Language = "es-ES", FontSize = 1800 });
                run.Append(new A.Text(line));

                paragraph.Append(run);
                paragraphs.Add(paragraph);
            }

            return paragraphs;
        }

    }
}

