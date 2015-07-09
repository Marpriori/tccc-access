using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using TcccAccess.Types;

namespace TcccAccess
{
    public partial class Form1 : Form
    {

        List<Types.Rota> listaRotas = new List<Types.Rota>();
        private Types.Rota rotaAtual = null;
        private int origemAtual = 0;
        private int diaAtual = 0; //Vai até 2
        private int situacao = 0;

        private const int SELECT_ROTA = 1;
        private const int SELECT_ORIGEM = 2;
        private const int SELECT_DIA = 3;
        private const int CLICK_BOTAO = 4;
        private const int SALVAR_DADOS = 5;
        private const int ACABOU = 6;

        

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            webBrowser1 = new WebBrowser();
            webBrowser1.Navigate("http://www.tccc.com.br");
            
        }

        private void WebBrowser1DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            listBox1.Items.Add(webBrowser1.Url);
            Refresh();
            RodarProcesso();

        }

        private void RodarProcesso()
        {
          bool eventoDid = false;
            if (listaRotas.Count == 0) CarregarListaRota();

            switch (situacao)
            {
                case SELECT_ROTA:
                   eventoDid = SelecionarRota();
                    break;
                case SELECT_ORIGEM:
                    eventoDid = SelecionarOrigem();
                    break;
                case SELECT_DIA:
                    SelecionarDia();
                    break;
                case CLICK_BOTAO:
                    ClickBotao();
                    eventoDid = true;
                    break;
                case SALVAR_DADOS:
                    SalvarDados();
                    break;
            }

            if(!eventoDid)
                RodarProcesso();
        }

        private void SalvarDados()
        {
            if (webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_lblNomeLinha") == null)
            {
                ClickBotao();
                return;
            }
            string nomeSentido = webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_lblNomeDestino").InnerHtml;
            
            Sentido sentido=null;
            
            foreach (Sentido sentido1 in rotaAtual.Sentidos.Where(sentido1 => sentido1.Nome == nomeSentido))
            {
                sentido = sentido1;
            }
            if(sentido == null)
            {
                sentido = new Sentido();
                rotaAtual.Sentidos.Add(sentido);    

            }
            sentido.Nome = nomeSentido;
            Dia dia = null;
            string nomeDia = webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_lblDia").InnerHtml;
            foreach ( Dia d in sentido.Dias.Where(d2=>d2.Nome == nomeDia))
            {
                dia = d;
            }
            if(dia == null)
            {
                dia = new Dia();
                sentido.Dias.Add(dia);
            }
            
            dia.Nome = nomeDia;

            string valorBase = string.Format("");
            listBox2.Items.Add(webBrowser1.Document.GetElementById(
                                               "ctl00_ContentPlaceHolder3_lblNomeLinha").InnerHtml);
            listBox3.Items.Add(webBrowser1.Document.GetElementById(
                "ctl00_ContentPlaceHolder3_lblNomeDestino").InnerHtml);
            listBox4.Items.Add(webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_lblDia").
                                   InnerHtml);
            listBox2.SelectedIndex = listBox2.Items.Count - 1;
            listBox3.SelectedIndex = listBox3.Items.Count - 1;
            listBox4.SelectedIndex = listBox4.Items.Count - 1;


            if (webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_GridView1") != null)
            {
                int x = 0;

                string valor = "";

                Horario horario = new Horario();
                foreach (
                    HtmlElement VARIABLE in
                        webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_GridView1").
                            GetElementsByTagName("td"))
                {

                    switch (x)
                    {
                        case 0: //partida
                            valor += VARIABLE.InnerHtml;
                            horario.Partida = VARIABLE.InnerHtml;
                            break;
                        case 1: //chegada
                            valor += " - " + VARIABLE.InnerHtml;
                            horario.Chegada = VARIABLE.InnerHtml;
                            break;
                        case 2: // obs
                            if (VARIABLE.InnerHtml != "&nbsp;")
                            {
                                valor += " - " + VARIABLE.InnerHtml;
                                horario.Observacao = VARIABLE.InnerHtml;
                            }

                            listBox1.Items.Add(valor);
                            valor = valorBase;
                            break;
                    }
                    if (x == 2)
                    {
                        dia.Horarios.Add(new Horario()
                                                 {
                                                     Chegada = horario.Chegada,
                                                     Partida = horario.Partida,
                                                     Observacao = horario.Observacao
                                                 });
                        horario = new Horario();
                        x = 0;
                    }
                    else x++;
                }

            }

            if (webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_GridView2") != null)
            {
                listBox2.Items.Add(valorBase);
                if (sentido.Etinerario.Count == 0)
                {
                    foreach (
                        HtmlElement VARIABLE in
                            webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder3_GridView2").
                                GetElementsByTagName("td"))
                    {
                        sentido.Etinerario.Add(VARIABLE.InnerHtml);
                        listBox2.Items.Add(VARIABLE.InnerHtml);
                    }
                }
            }
            if (origemAtual < rotaAtual.Origens.Count)
            {
                origemAtual++;
                situacao = SELECT_ORIGEM;
            }
            else
            {
                origemAtual = 0;
                JSONSerialize(rotaAtual);
                listBox1.Refresh();
                listBox2.Refresh();
                situacao = SELECT_ROTA;
            }
            return;

        }

        private void JSONSerialize(Rota rotaAtual)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer jsonSer = new DataContractJsonSerializer(typeof(Rota));
            jsonSer.WriteObject(stream, rotaAtual);
            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            File.WriteAllText(string.Format("D:/rota/{1} - {0}.json", rotaAtual.Nome, DateTime.Now.ToString("yyyy_MM_dd_HHmmss")), sr.ReadToEnd(), Encoding.UTF8);
        }

        private void ClickBotao()
        {
            HtmlElement botalEl = webBrowser1.Document.GetElementById("UChorarios1_btnConsultar");
            if (botalEl == null)
                botalEl = webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder2_UChorarios1_btnConsultar");
            situacao = SALVAR_DADOS;
            botalEl.InvokeMember("click");
        }

        private void SelecionarDia()
        {
            HtmlElement diaEl = webBrowser1.Document.GetElementById("UChorarios1_dropTipo");
            if(diaEl == null) diaEl = webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder2_UChorarios1_dropTipo");

            if (diaAtual > 2)
                diaAtual = 0;

            if(diaEl.GetAttribute("SelectedIndex") != diaAtual.ToString())
            {
                diaEl.SetAttribute("SelectedIndex", diaAtual.ToString());
            }
            situacao = CLICK_BOTAO;
           // RodarProcesso();

        }

        private bool SelecionarOrigem()
        {
            bool eventoDid = false;
            #region Carregar Origens

            HtmlElement origemEl = webBrowser1.Document.GetElementById("UChorarios1_dropOrigem");
            if (origemEl == null)
                origemEl = webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder2_UChorarios1_dropOrigem");
            if (rotaAtual.Origens.Count == 0)
            {
                if (origemEl != null)
                {
                    foreach (HtmlElement el in origemEl.GetElementsByTagName("option"))
                    {
                       // listBox1.Items.Add(el.InnerHtml);
                        rotaAtual.Origens.Add(new ListItem(el.InnerText, el.GetAttribute("value")));
                    }
                }
            }
            if (origemEl != null)
            {
                string selected = origemEl.GetAttribute("selectedIndex");
                if (selected != origemAtual.ToString())
                {
                    origemEl.SetAttribute("SelectedIndex", origemAtual.ToString());
                    
                    
                    eventoDid = true;
                    origemEl.RaiseEvent("onChange");
                }
                //origemAtual++;
                situacao = SELECT_DIA;
            }

            #endregion Carregar Origens

            return eventoDid;
            // situacao = SELECT_DIA;
            //RodarProcesso();
        }

        private bool SelecionarRota()
        {
            bool eventoDid = false;
            //CARREGAR ROTA
            #region CarregarRota
            bool proxima = false;
            if (rotaAtual == null)
            {
                rotaAtual = listaRotas.First();
            }
            else
            {
                if (rotaAtual.Codigo == listaRotas.Last().Codigo && diaAtual == 2)
                {

                    situacao = ACABOU;
                    return true;


                }
                else
                {
                    if (rotaAtual.Codigo == listaRotas.Last().Codigo)
                    {
                        diaAtual++;
                        rotaAtual = listaRotas.First();
                    }
                    else
                    {
                        foreach (Types.Rota rota in listaRotas)
                        {
                            if (proxima)
                            {
                                rotaAtual = rota;
                                break;
                            }
                            if (rota.Codigo == rotaAtual.Codigo) proxima = true;
                        }
                    }
                }
            }
            comboBox1.SelectedIndex = comboBox1.FindString(rotaAtual.Codigo);
            #endregion CarregarRota

            #region SET Rota WEb
            HtmlElement rotaEl = webBrowser1.Document.GetElementById("UChorarios1_droLinha");
            if (rotaEl == null) rotaEl = webBrowser1.Document.GetElementById("ctl00_ContentPlaceHolder2_UChorarios1_droLinha");
            if(rotaEl != null)
            {
                int i = 0;
                foreach (HtmlElement element in rotaEl.GetElementsByTagName("option"))
                {
                    if (element.GetAttribute("value") == rotaAtual.Codigo) break;
                    i++;
                }
                situacao = SELECT_ORIGEM;
                rotaEl.SetAttribute("SelectedIndex", i.ToString());
                eventoDid = true;
                rotaEl.RaiseEvent("onChange");
            }
            #endregion

            return eventoDid;
        }

        private void CarregarListaRota()
        {
            if (webBrowser1.Document != null)
            {
                HtmlElement el = webBrowser1.Document.GetElementById("UChorarios1_droLinha");
                if (el != null)
                {
                    foreach (HtmlElement element in el.GetElementsByTagName("option"))
                    {
                        string value = element.GetAttribute("value");
                        if (string.IsNullOrEmpty(value) || value == "SELECIONE" || value == "0") continue;

                        listaRotas.Add(new Types.Rota() { Nome = element.InnerText, Codigo = value });
                    }
                }
            }

            comboBox1.DataSource = listaRotas;
            comboBox1.DisplayMember = "Nome";
            comboBox1.ValueMember = "Codigo";
            comboBox1.Refresh();

            situacao = SELECT_ROTA;
        }
    }

    namespace Types
    {
    public class Rota
    {
        public Rota()
        {
            Origens = new List<ListItem>();
            Sentidos = new List<Sentido>();
        }
        public string Codigo { get; set; }
        public string Nome { get; set; }

        public List<ListItem> Origens { get; set; }
        public List<Sentido> Sentidos { get; set; } 
    }

        public class Sentido
        {
            public Sentido()
            {
                Dias = new List<Dia>();
               
                Etinerario = new List<string>();
            }
            public List<string> Etinerario { get; set; } 
            public string Nome { get; set; }
            public List<Dia> Dias { get; set; } 
            
            
        }
        public class Dia
        {
            public Dia()
            {
                Horarios = new List<Horario>(); 
         
            }
            public string Nome { get; set; }
            public List<Horario> Horarios { get; set; }
           
        }
        public class Horario
        {
            public string Partida { get; set; }
            public string Chegada { get; set; }
            public string Observacao { get; set; }
        }


}
}
