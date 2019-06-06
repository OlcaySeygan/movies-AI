using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;

using oLseyLibrary.Mathematics;
using oLseyLibrary.Mathematics.Functions;

using oLseyLibrary.Model;
using System.Diagnostics;
using System.Net;
using oLseyLibrary;

namespace WindowsFormsApp {
    public partial class formUI : Form {

        Stopwatch stopwatch = new Stopwatch();

        float padding = 0.1f;
        Size size = new Size((int)(50 * 2.5), (int)(75 * 2.5));
        
        public formUI() {
            InitializeComponent();

            this.Paint += FormUI_Paint;
            this.KeyPress += FormUI_KeyPress;
        }

        private void FormUI_KeyPress(object sender, KeyPressEventArgs e) {
            if ((int)e.KeyChar == 27)
                Application.Exit();
        }
        
        private void FormUI_Paint(object sender, PaintEventArgs e) {
            Graphics g = CreateGraphics();

            Rectangle left = new Rectangle(new Point(0, 0), new Size((int)(this.Width * padding), this.Height));
            Rectangle right = new Rectangle(new Point((int)(this.Width - this.Width * padding), 0), new Size((int)(this.Width * padding), this.Height));
            
            g.FillRectangle(Brushes.Firebrick, left);
            g.FillRectangle(Brushes.ForestGreen, right);

            g.DrawRectangle(Pens.Black, left);
            g.DrawRectangle(Pens.Black, right);
            
            g.Dispose();
        }

        NeuralNetwork neuralNetwork;
        Regression trainingData = new Regression();

        List<string> linesList = new List<string>();
        List<PictureBox> pictureBoxes = new List<PictureBox>();

        List<string> categories = new List<string>();
        List<string> directors = new List<string>();
        List<string> stars = new List<string>();

        List<string> months = new List<string>();
        
        private void GetCast(string url) {

            // Get Cast
            WebBrowser webBrowser = new WebBrowser() {
                ScriptErrorsSuppressed = true
            };
            
            webBrowser.Navigate(url);
            while (webBrowser.ReadyState != WebBrowserReadyState.Complete) {
                System.Windows.Forms.Application.DoEvents();
            }

            HtmlElement doc = webBrowser.Document.GetElementById("root");
            string innerText = doc.InnerText;

            string[] stringSeparatorsHTML = new string[] { "\r\n" };
            string[] stringSeparatorsName = new string[] { " ... " };
            string[] splitHTML = innerText.Split(stringSeparatorsHTML, StringSplitOptions.RemoveEmptyEntries);

            List<string> names = new List<string>();

            bool searhing = false;

            for (int i = 0; i < splitHTML.Length; i++) {
                if (splitHTML[i] == "Cast (in credits order)   " || splitHTML[i] == "Cast (in credits order) verified as complete   " || splitHTML[i] == "Cast   " || splitHTML[i] == "Cast (in credits order) complete, awaiting verification   ") {
                    searhing = true;
                    continue;
                } else if (splitHTML[i] == "Rest of cast listed alphabetically:" || (splitHTML[i].Length >= 11 && splitHTML[i].Substring(0, 11) == "Produced by")) {
                    searhing = false;
                    break;
                }

                if (searhing)
                    names.Add(splitHTML[i].ToLower().Split(stringSeparatorsName, StringSplitOptions.RemoveEmptyEntries)[0]);
            }
            string output = "";
            for (int i = 0; i < names.Count; i++) {
                names[i] = names[i].Trim();
                names[i] = names[i].Replace('ı', 'i');
                names[i] = names[i].Replace('ö', 'o');
                names[i] = names[i].Replace('ş', 's');
                names[i] = names[i].Replace('ü', 'u');
                names[i] = names[i].Replace('ç', 'c');
                names[i] = names[i].Replace(".", "");
                names[i] = names[i].Replace(' ', '.');
                output += i == 0 ? names[i] : "," + names[i];
            }
            Clipboard.SetText(output);

            Application.Exit();
        }

        private void formUI_Load(object sender, EventArgs e) {
            
            ToolTip toolTip = new ToolTip();
            
            months.Add("January");
            months.Add("February");
            months.Add("March");
            months.Add("April");
            months.Add("May");
            months.Add("June");
            months.Add("July");
            months.Add("August");
            months.Add("September");
            months.Add("October");
            months.Add("November");
            months.Add("December");

            string path = @"images\";
            
            string[] lines = File.ReadAllLines(path + "data.txt");
            linesList.AddRange(lines);
            
            for (int i = 0; i < lines.Length; i++) {
                string[] split = lines[i].Split(' ');

                string name = split[0];                

                PictureBox pictureBoxPoster = new PictureBox() {
                    Name = Path.GetFileName(name),
                    Location = new Point((int)Map.Float(0f,
                                                    RandomF.NextFloat(this.Width),
                                                    this.Width,
                                                    this.Width * padding,
                                                    this.Width - (this.Width * padding) - size.Width),
                                                    RandomF.Next(this.Height - size.Height)),
                    Image = Image.FromFile(path + name),
                    Size = size,
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Tag = i.ToString(),
                    BorderStyle = BorderStyle.FixedSingle
                };

                pictureBoxPoster.MouseDown += PictureBoxPoster_MouseDown;
                pictureBoxPoster.MouseUp += PictureBoxPoster_MouseUp;
                pictureBoxPoster.MouseMove += PictureBoxPoster_MouseMove;

                toolTip.SetToolTip(pictureBoxPoster, split[0]);

                pictureBoxes.Add(pictureBoxPoster);
                this.Controls.Add(pictureBoxPoster);

                // Categories
                string[] categoriesArr = split[4].Split(',');
                for (int j = 0; j < categoriesArr.Length; j++) {
                    if (!categories.Contains(categoriesArr[j])) {
                        categories.Add(categoriesArr[j]);
                    }
                }

                // Directors
                string[] directorsArr = split[5].Split(',');
                for (int j = 0; j < directorsArr.Length; j++) {
                    if (!directors.Contains(directorsArr[j])) {
                        directors.Add(directorsArr[j]);
                    }
                }

                // Stars
                string[] starsArr = split[6].Split(',');
                for (int j = 0; j < starsArr.Length; j++) {
                    if (!stars.Contains(starsArr[j])) {
                        stars.Add(starsArr[j]);
                    }
                }
            }
            
            neuralNetwork = new NeuralNetwork(31 + 12 + 1 + categories.Count + directors.Count + stars.Count, 64, 1) {
                max_circle=10000,
                learning_rate=0.5f,
                momentum_rate=0.96f
            };    
            
        }

        bool toggleMove = false;
        Point offset;
        Point lastPoint;

        private void PictureBoxPoster_MouseMove(object sender, MouseEventArgs e) {
            if (toggleMove) {
                var relativePoint = this.PointToClient(Cursor.Position);
                PictureBox pictureBox = (PictureBox)sender;
                pictureBox.Location = new Point(relativePoint.X - offset.X, relativePoint.Y - offset.Y);
            }
        }

        bool existRight = false;
        bool existLeft = false;

        private void PictureBoxPoster_MouseUp(object sender, MouseEventArgs e) {
            toggleMove = false;
            PictureBox pictureBox = (PictureBox)sender;
            bool train = false;
            float value = 0f;
            if (pictureBox.Location.X >= (this.Width - (this.Width * padding))) {
                value = 1f;
                train = true;
                existRight = true;
            } else if (pictureBox.Location.X <= (this.Width * padding - size.Width) ) {
                value = 0f;
                train = true;
                existLeft = true;
            } else if (trainingData.Length > 0 && (existLeft && existRight)) {
                pictureBox.Location = new Point(lastPoint.X, pictureBox.Location.Y);                
                pictureBox.BringToFront();
            }

            if(train) {
                string[] split = linesList[int.Parse(pictureBox.Tag.ToString())].Split(' ');

                int day = int.Parse(split[1]) - 1;
                int month = months.IndexOf(split[2]);
                float year = Map.Float(0f, float.Parse(split[3]), 9999f, 0f, 1f);

                List<float> inputs = new List<float>();
                for (int i = 0; i < 31; i++) {
                    if (i == day)
                        inputs.Add(1f);
                    else
                        inputs.Add(0f);
                }

                for (int i = 0; i < 12; i++) {
                    if (i == month)
                        inputs.Add(1f);
                    else
                        inputs.Add(0f);
                }
                inputs.Add(year);
                
                string[] categoriesArr = split[4].Split(',');
                for (int i = 0; i < categories.Count; i++) {
                    bool exist = false;
                    for (int j = 0; j < categoriesArr.Length; j++) {
                        if(categories[i] == categoriesArr[j]) {
                            exist = true;
                            break;
                        }
                    }
                    if(exist) {
                        inputs.Add(1f);
                    } else {
                        inputs.Add(0f);
                    }
                }

                string[] directorsArr = split[5].Split(',');
                for (int i = 0; i < directors.Count; i++) {
                    bool exist = false;
                    for (int j = 0; j < directorsArr.Length; j++) {
                        if (directors[i] == directorsArr[j]) {
                            exist = true;
                            break;
                        }
                    }
                    if (exist) {
                        inputs.Add(1f);
                    } else {
                        inputs.Add(0f);
                    }
                }

                string[] starsArr = split[6].Split(',');
                for (int i = 0; i < stars.Count; i++) {
                    bool exist = false;
                    for (int j = 0; j < starsArr.Length; j++) {
                        if (stars[i] == starsArr[j]) {
                            exist = true;
                            break;
                        }
                    }
                    if (exist) {
                        inputs.Add(1f);
                    } else {
                        inputs.Add(0f);
                    }
                }

                trainingData.Add(inputs.ToArray(), new[] { value });
                stopwatch.Start();
                neuralNetwork.Training(trainingData);
                stopwatch.Stop();
                Console.WriteLine("Training : " + stopwatch.ElapsedMilliseconds);
                stopwatch.Reset();

                pictureBox.MouseDown -= PictureBoxPoster_MouseDown;
                pictureBox.MouseUp -= PictureBoxPoster_MouseUp;
                pictureBox.MouseMove -= PictureBoxPoster_MouseMove;
                
                pictureBoxes.Remove(pictureBox);

                if(existLeft && existRight) {
                    for (int i = 0; i < pictureBoxes.Count; i++) {

                        split = linesList[int.Parse(pictureBoxes[i].Tag.ToString())].Split(' ');

                        day = int.Parse(split[1]) - 1;
                        month = months.IndexOf(split[2]);
                        year = Map.Float(1f, float.Parse(split[3]), 9999f, 0f, 1f);

                        inputs = new List<float>();
                        for (int j = 0; j < 31; j++) {
                            if (j == day)
                                inputs.Add(1f);
                            else
                                inputs.Add(0f);
                        }

                        for (int j = 0; j < 12; j++) {
                            if (j == month)
                                inputs.Add(1f);
                            else
                                inputs.Add(0f);
                        }
                        inputs.Add(year);

                        categoriesArr = split[4].Split(',');
                        for (int j = 0; j < categories.Count; j++) {
                            bool exist = false;
                            for (int z = 0; z < categoriesArr.Length; z++) {
                                if (categories[j] == categoriesArr[z]) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {
                                inputs.Add(1f);
                            } else {
                                inputs.Add(0f);
                            }
                        }

                        directorsArr = split[5].Split(',');
                        for (int j = 0; j < directors.Count; j++) {
                            bool exist = false;
                            for (int z = 0; z < directorsArr.Length; z++) {
                                if (directors[j] == directorsArr[z]) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {
                                inputs.Add(1f);
                            } else {
                                inputs.Add(0f);
                            }
                        }

                        starsArr = split[6].Split(',');
                        for (int j = 0; j < stars.Count; j++) {
                            bool exist = false;
                            for (int z = 0; z < starsArr.Length; z++) {
                                if (stars[j] == starsArr[z]) {
                                    exist = true;
                                    break;
                                }
                            }
                            if (exist) {
                                inputs.Add(1f);
                            } else {
                                inputs.Add(0f);
                            }
                        }

                        stopwatch.Start();
                        Result[] guesses = neuralNetwork.Predict(inputs.ToArray());
                        stopwatch.Stop();
                        Console.WriteLine("Predict:" + stopwatch.ElapsedMilliseconds);
                        stopwatch.Reset();
                        
                        pictureBoxes[i].Location = new Point((int)Map.Float(0f,
                                                    guesses[0].value,
                                                    1f,
                                                    this.Width * padding,
                                                    this.Width - (this.Width * padding) - size.Width),
                                                    (int)RandomF.NextFloat(this.Height - size.Height));
                    }
                }
            }
        }
        
        private void PictureBoxPoster_MouseDown(object sender, MouseEventArgs e) {
            toggleMove = true;
            PictureBox pictureBox = (PictureBox)sender;
            pictureBox.BringToFront();
            offset = new Point(e.X, e.Y);
            lastPoint = pictureBox.Location;
        }
        
    }
}
