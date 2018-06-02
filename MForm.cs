using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using Corsis.Xhtml;
using System.Collections;
using System.Diagnostics;

namespace ePubTxt
{
    public partial class MForm :  ComponentFactory.Krypton.Toolkit.KryptonForm
    {
        pdfengine.acropdf PDFEngine = new pdfengine.acropdf();
        string curErrLog = "";
        BackgroundWorker _bw;
        delegate void update_probar(string text, int max, int cur);
        public MForm()
        {
            InitializeComponent();
        }

        private void MForm_Load(object sender, EventArgs e)
        {
           
            this.Text += " " + Application.ProductVersion;

            gCls.update_path_var();
        }

        private void kryptonButton1_Click(object sender, EventArgs e)
        {

            try
            {
                if (txt_inpdf.Text == "") {
                    gCls.show_error("Select PDF file");
                    return;
                }
                if (!File.Exists(txt_inpdf.Text)) {
                    gCls.show_error("File not found");
                    return;
                }

                if (txt_epubcover.Text != "") {
                    if (!File.Exists(txt_epubcover.Text)) {
                        gCls.show_error("Cover image file not found");
                        return;
                    }
                }

                string rootDir = Application.StartupPath + "\\in_pdf";
                if (Directory.Exists(rootDir)) {
                    Directory.Delete(rootDir, true);                     
                }

                Directory.CreateDirectory(rootDir);

                string outPath = Path.GetDirectoryName(txt_inpdf.Text);
                string fNameWOE = Path.GetFileNameWithoutExtension(txt_inpdf.Text);

                Updf pdf_info = new Updf(fNameWOE, txt_booktitle.Text, txt_bookauthor.Text, txt_inpdf.Text, rootDir, outPath, txt_epubcover.Text);

                curErrLog = "";
               
                _bw = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };
                _bw.DoWork += bw_DoWork;
                probar.Visible = true;
                _bw.ProgressChanged += bw_ProgressChanged;
                _bw.RunWorkerCompleted += bw_RunWorkerCompleted;

                             
                _bw.RunWorkerAsync(pdf_info);


                
            }
            catch (Exception erd) {
                gCls.show_error(erd.Message.ToString());
                return;
            }

        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            Updf live_pdf_info = (Updf)e.Argument;
            convert_epub(live_pdf_info);

        }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            probar.Visible = false;
            lbstatus.Text = "";
            if (curErrLog != "")
            {
                gCls.show_error(curErrLog);
                return;
            }
            else
            {
                
                gCls.show_message("ePub converted successfully.");
            }
        }
        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
        public void probar_update(string Etext, int max, int cur)
        {
            try
            {
                if (probar.InvokeRequired)
                {
                    update_probar up = new update_probar(probar_update);
                    this.Invoke(up, new object[] { Etext, max, cur });
                }
                else
                {
                    if (Etext != "")
                    {
                        lbstatus.Text = Etext;
                    }
                    probar.Maximum = max;
                    probar.Value = cur;
                }

            }
            catch { }

        }

        public void convert_epub(Updf cPDF) {

            try
            {
                string rootPath = cPDF.b_rootpath;
                string outPath = cPDF.b_outpath;
                string fnameWOE = cPDF.b_filenamewoe;
                string pdfFile = cPDF.b_pdffile;

                probar_update("Create Directory...", 10, 0);

                #region create dir
                string wrkDir = rootPath;
                if (!Directory.Exists(wrkDir + "\\META-INF"))
                {
                    Directory.CreateDirectory(wrkDir + "\\META-INF");
                }
                if (!Directory.Exists(wrkDir + "\\OEBPS"))
                {
                    Directory.CreateDirectory(wrkDir + "\\OEBPS");
                }
                string oebDir = wrkDir + "\\OEBPS";
                if (!Directory.Exists(oebDir + "\\html"))
                {
                    Directory.CreateDirectory(oebDir + "\\html");
                }
                //if (!Directory.Exists(oebDir + "\\images"))
                //{
                //    Directory.CreateDirectory(oebDir + "\\images");
                //}
                if (!Directory.Exists(oebDir + "\\styles"))
                {
                    Directory.CreateDirectory(oebDir + "\\styles");
                }
                #endregion

                string imgDir = oebDir + "\\images";
                string styDir = oebDir + "\\styles";
                string htmlDir = oebDir + "\\html";


                probar_update("Generate html...", 10, 3);

                gCls.FindAndKillProcess("Acrobat.exe");

                PDFEngine.CloseAlldoc();
                int pCount = 0;
                string cTxt = PDFEngine.ConvertHtml(pdfFile, oebDir + "\\001.html",ref pCount);
                if (cTxt != "")
                {
                    curErrLog += cTxt + "\nAdobe Acrobat 7 or later should be installed in your system.";                    
                    return;
                }

                if (!File.Exists(oebDir + "\\001.html")) {
                    curErrLog += "Unable to generate html";                    
                    return;
                }

                #region cover image

                if (cPDF.b_coverpath != "")
                {
                    File.Copy(cPDF.b_coverpath, imgDir + "\\cover.jpg", true);
                }
                else
                {
                    File.Copy(Application.StartupPath + "\\template\\cover.jpg", imgDir + "\\cover.jpg", true);
                }
                #endregion

                string b_title = cPDF.b_title;
                string b_author = cPDF.b_author;

                probar_update("xhtml conversion...", 10, 6);
                //html edit
                #region html edit
                string tHtml = File.ReadAllText(oebDir + "\\001.html");
                tHtml = tHtml.Replace("\r\n", "");
                tHtml = tHtml.Replace("\r", "");
                tHtml = tHtml.Replace("\n", "");

                tHtml = tHtml.Replace("<!-- Created from PDF via Acrobat SaveAsXML --><!-- Mapping table version: 28-February-2003 -->", "");
                Regex rgx = new Regex("<STYLE\\stype=\"text/css\">(.+?)</STYLE>", RegexOptions.IgnoreCase);
                tHtml = rgx.Replace(tHtml, "");
                rgx = new Regex("<DIV\\sclass=\"(.+?)\">", RegexOptions.IgnoreCase);
                tHtml = rgx.Replace(tHtml, "");
                rgx = new Regex("</DIV>", RegexOptions.IgnoreCase);
                tHtml = rgx.Replace(tHtml, "");
                var xhtmlTxt = Html2Xhtml.RunAsFilter(stdin => stdin.Write(tHtml)).ReadToEnd();
                tHtml = xhtmlTxt;

                tHtml = tHtml.Replace("\r\n", "");
                tHtml = tHtml.Replace("\r", "");                
                tHtml = tHtml.Replace("\n", "");
                #endregion

                #region get author and title
                rgx = new Regex("<meta\\sname=\"dc.creator\"\\scontent=\"(.+?)\"", RegexOptions.IgnoreCase);

                if (b_author == "") { 
                 if(rgx.Match(tHtml).Success){
                     b_author = rgx.Match(tHtml).Groups[1].Value.ToString();
                 }
                }

                rgx = new Regex("<meta\\sname=\"dc.title\"\\scontent=\"(.+?)\"", RegexOptions.IgnoreCase);
                if (b_title == "")
                {
                    if (rgx.Match(tHtml).Success)
                    {
                        b_title = rgx.Match(tHtml).Groups[1].Value.ToString();
                    }
                }
                #endregion

                #region edit1

                rgx = new Regex("<head>(.+?)</head>", RegexOptions.IgnoreCase);
                tHtml = rgx.Replace(tHtml, "<head><title>" + b_title  + "</title></head>");

                rgx = new Regex("<body><hr\\s/><ul>(.+?)</ul>", RegexOptions.IgnoreCase);

                #region get toc content
                string tocTxt = "";
                if (rgx.Match(tHtml).Success) {
                    tocTxt = rgx.Match(tHtml).Groups[1].Value.ToString();
                    tHtml = rgx.Replace(tHtml, "<body>");
                }

             

                #endregion

                rgx = new Regex("<!DOCTYPE\\shtml(.+?)>", RegexOptions.IgnoreCase);
                tHtml = rgx.Replace(tHtml, "");

                #region get style
                int sPoint = 0;
                ArrayList styList = new ArrayList();
                while (tHtml.IndexOf("style=\"", sPoint) != -1)
                {
                    int iPoint = tHtml.IndexOf("style=\"", sPoint);
                    int ePoint = tHtml.IndexOf("\"", iPoint + 7) + 1;
                    string sTxt = tHtml.Substring(iPoint, ePoint - iPoint);
                    tHtml = tHtml.Remove(iPoint, ePoint - iPoint);

                    sTxt = sTxt.Replace("style=\"", "");
                    sTxt = sTxt.Replace("\"", "");
                    string sFound = "";
                    for (int s = 0; s < styList.Count; s++)
                    {
                        if (styList[s].ToString() == sTxt)
                        {
                            sFound = "sty" + s.ToString();
                        }
                    }

                    if (sFound == "")
                    {
                        styList.Add(sTxt);
                        sFound = "sty" + (styList.Count - 1).ToString();
                    }

                    tHtml = tHtml.Insert(iPoint, "class=\"" + sFound + "\"");

                    sPoint = iPoint + 5;

                }

                #endregion


                #region style writte

                string cssTxt = "";

                for (int s = 0; s < styList.Count; s++)
                {
                    cssTxt += ".sty" + s.ToString() + " {\n";
                    cssTxt += styList[s] + "\n";
                    cssTxt += "}\n";
                }
                File.WriteAllText(styDir + "\\page.css", cssTxt);
                #endregion

                //images
                tHtml = tHtml.Replace("src=\"images/", "src=\"../images/");
                tHtml = tHtml.Replace("<head>", "<head>\n<link rel=\"stylesheet\" type=\"text/css\" href=\"../styles/page.css\" />");

                ArrayList pageList = new ArrayList();
                #region split page
                rgx = new Regex("<body>(.+?)</body>", RegexOptions.IgnoreCase);
                if (rgx.Match(tHtml).Success) {
                    string mBody = rgx.Match(tHtml).Groups[1].Value.ToString();
                    sPoint = 0;
                    while (mBody.IndexOf("id=\"LinkTarget_", sPoint) != -1) {
                        int iPoint = mBody.IndexOf("id=\"LinkTarget_", sPoint);
                        if (iPoint > 15) {
                            if (mBody.LastIndexOf("</", iPoint) != -1) {
                                int m1 = mBody.LastIndexOf("</", iPoint);
                                int m2 = mBody.IndexOf(">", m1) + 1;
                                string getX = mBody.Substring(0, m2);
                                mBody = mBody.Remove(0, m2);
                                pageList.Add(getX);
                                iPoint = mBody.IndexOf("id=\"LinkTarget_");
                            }
                        }                       
                        sPoint = iPoint + 5;

                    }

                    pageList.Add(mBody);
                
                }

                ArrayList pageFileList = new ArrayList();

                for (int p = 0; p < pageList.Count; p++) {
                    string pgTxt = pageList[p].ToString();
                    int lp = p + 1;
                    pgTxt = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\"  \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\" >\n <html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:ibooks=\"http://apple.com/ibooks/html-extensions\" xmlns:epub=\"http://www.idpf.org/2007/ops\">\n<head><link rel=\"stylesheet\" type=\"text/css\" href=\"../styles/page.css\" />\n<title>" + b_title + "</title>\n</head>\n<body>" + pgTxt + "\n</body>\n</html>";

                    xhtmlTxt = Html2Xhtml.RunAsFilter(stdin => stdin.Write(pgTxt)).ReadToEnd();
                    pgTxt = xhtmlTxt;
                    rgx = new Regex("<html\\sxmlns=\"(.+?)\">", RegexOptions.IgnoreCase);
                    pgTxt = rgx.Replace(pgTxt, "<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:ibooks=\"http://apple.com/ibooks/html-extensions\" xmlns:epub=\"http://www.idpf.org/2007/ops\">");

                    string htmlFile = "";
                    if (lp.ToString().Length == 1) {
                        htmlFile = "00" + lp.ToString() + ".html";
                    }
                    else if (lp.ToString().Length == 2)
                    {
                        htmlFile = "0" + lp.ToString() + ".html";
                    }
                    else {
                        htmlFile =  lp.ToString() + ".html";
                    }
                    pageFileList.Add(htmlFile);

                    File.WriteAllText(htmlDir + "\\" + htmlFile, pgTxt);
                    
                }


                File.Delete(oebDir + "\\001.html");
                
              
              

                #endregion


              

                #endregion




                probar_update("Metadata generation...", 10, 7);

                File.Copy(Application.StartupPath + "\\template\\META-INF\\container.xml", wrkDir + "\\META-INF\\container.xml", true);
                File.Copy(Application.StartupPath + "\\template\\mimetype", wrkDir + "\\mimetype", true);

                #region toc.ncx
                string tocX = File.ReadAllText(Application.StartupPath + "\\template\\OEBPS\\toc.ncx");
                tocX = tocX.Replace("<docTitle><text></text></docTitle>", "<docTitle><text>" + b_title + "</text></docTitle>");
                tocX = tocX.Replace("<docAuthor><text></text></docAuthor>", "<docAuthor><text>" + b_author + "</text></docAuthor>");
                string mtocX = "";
               
                string wtocTxt = "";
                if (tocTxt == "")
                {
                    mtocX += "<navPoint id=\"p1\" playOrder=\"1\">\n";
                    mtocX += "<navLabel><text>" + b_title + "</text></navLabel>\n";
                    mtocX += "<content src=\"html/001.html\" />\n";
                    mtocX += "</navPoint>\n";
                }
                else {
                   
                    rgx = new Regex("<a\\shref=\"(.+?)\">(.+?)</a>", RegexOptions.IgnoreCase);
                    MatchCollection tmcol = rgx.Matches(tocTxt);
                    int tIndex = 1;
                    foreach (Match m in tmcol) {
                        string linkTxt = m.Groups[1].Value.ToString();
                        linkTxt = linkTxt.Replace("#", "");
                        string pageLink = "";
                        for (int j = 0; j < pageList.Count; j++) {
                            if (pageList[j].ToString().IndexOf(linkTxt) != -1) {
                                pageLink = pageFileList[j].ToString();
                            }
                        }

                        //toc.html
                        wtocTxt += "<li><a href=\"" + pageLink + m.Groups[1].Value.ToString() + "\">" + m.Groups[2].Value.ToString() + "</a></li>\n"; 
                        
                        mtocX += "<navPoint id=\"p" + tIndex.ToString() + "\" playOrder=\"" + tIndex.ToString() + "\">\n";
                        mtocX += "<navLabel><text>" + m.Groups[2].Value.ToString() + "</text></navLabel>\n";
                        mtocX += "<content src=\"html/" + pageLink + m.Groups[1].Value.ToString() + "\" />\n";
                        mtocX += "</navPoint>\n";
                        tIndex++;
                    }
                
                }

                tocX = tocX.Replace("</ncx>", "<navMap>" +  mtocX + "</navMap>\n</ncx>");
                File.WriteAllText(oebDir + "\\toc.ncx", tocX);

                string tocw2 = "<!DOCTYPE html>\n<html xmlns=\"http://www.w3.org/1999/xhtml\">\n<head><title></title>\n</head><body>\n";
                tocw2 += "<ul>" + wtocTxt  + "</ul>\n</body>\n</html>";
                File.WriteAllText(htmlDir + "\\toc.html", tocw2);
               
                #endregion

                #region content.opf
                string contX = "<manifest>\n";
                //contX += "<item id=\"cover\" href=\"html/cover.html\" media-type=\"application/xhtml+xml\" />\n";
                if (tocTxt != "")
                {
                    contX += "<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\" />\n";
                }
                contX += "<item id=\"toc\" href=\"html/toc.html\" media-type=\"application/xhtml+xml\" />\n";
                contX += "<item id=\"pagecommon\" href=\"styles/page.css\" media-type=\"text/css\" />\n";

                for (int l = 0; l < pageFileList.Count; l++)
                {
                    contX += "<item id=\"page" + (l + 1).ToString()  + "\" href=\"html/" + pageFileList[l].ToString()  + "\" media-type=\"application/xhtml+xml\" />\n";
                }

                string[] doc_images = Directory.GetFiles(imgDir, "*.jpg");

                for (int p = 0; p < doc_images.Length; p++)
                {
                    string imgFileName = Path.GetFileName(doc_images[p]);
                    if (imgFileName != "cover.jpg")
                    {
                        contX += "<item id=\"image" + p.ToString() + "\" href=\"images/" + imgFileName + "\" media-type=\"image/jpeg\" />\n";
                    }
                }
                contX += "<item id=\"cover-image\" href=\"images/cover.jpg\" media-type=\"image/jpeg\" />\n";
                contX += "</manifest>\n";
                contX += "<spine toc=\"ncx\">\n";
               // contX += "<itemref idref=\"cover\" />\n";
                if (tocTxt != "")
                {
                    contX += "<itemref idref=\"toc\" />\n";
                }

                for (int t = 0; t < pageFileList.Count; t++)
                {
                    contX += "<itemref idref=\"page"  + (t + 1).ToString() + "\" />\n";
                }
                
                contX += "</spine>\n";

                string rcont = File.ReadAllText(Application.StartupPath + "\\template\\OEBPS\\content.opf");
                rcont = rcont.Replace("<dc:title></dc:title>", "<dc:title>" + b_title + "</dc:title>");
                rcont = rcont.Replace("<dc:creator></dc:creator>", "<dc:creator>" + b_author + "</dc:creator>");
                rcont = rcont.Replace("<dc:publisher></dc:publisher>", "<dc:publisher>" + b_author + "</dc:publisher>");
                rcont = rcont.Replace("<dc:rights></dc:rights>", "<dc:rights>" + b_author + "</dc:rights>");
                string mdyDate = DateTime.Today.ToString("yyyy-MM-dd");
                rcont = rcont.Replace("<dc:date></dc:date>", "<dc:date>" + mdyDate + "</dc:date>");

                rcont = rcont.Replace("</metadata>", "</metadata>" + contX);
                File.WriteAllText(oebDir + "\\content.opf", rcont);
                #endregion

              
                           


                string outepubPath = outPath + "\\" + fnameWOE + ".epub";

                if (File.Exists(outepubPath)) {
                    File.Delete(outepubPath);
                }

                string[] content = { wrkDir + "\\mimetype", wrkDir + "\\OEBPS", wrkDir + "\\META-INF" };

                probar_update("ePub Package creation...", 10, 9);

                //epub package
                #region epub package
                ProcessStartInfo psInfo = new ProcessStartInfo();
                Directory.SetCurrentDirectory(wrkDir);
                psInfo.CreateNoWindow = true;
                psInfo.UseShellExecute = false;
                psInfo.RedirectStandardOutput = true;
                psInfo.WindowStyle = ProcessWindowStyle.Hidden;
                psInfo.FileName = "ezip.exe";
                psInfo.Arguments = " -Xr9D \"" + outepubPath + "\" mimetype *";
                //psInfo.Arguments = "-Xr9D " + cPDF.b_filename + ".epub mimetype *";
                try
                {

                    using (Process exeProcess = Process.Start(psInfo))
                    {
                        string outE = exeProcess.StandardOutput.ReadToEnd();
                        exeProcess.WaitForExit();
                    }
                }
                catch (Exception erd)
                {
                    curErrLog += erd.Message.ToString();
                    
                }

                Directory.SetCurrentDirectory(Application.StartupPath);

          
                #endregion


               
                probar_update("Delete temp folder...", 10, 10);

                Directory.Delete(rootPath, true);

                
              
             
                

            }
            catch (Exception erd) {
                curErrLog += erd.Message.ToString();
                return;
            }
        
        
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                gCls.show_message(Application.ProductName + " " + Application.ProductVersion + "\nSend your Feedbacks to : vickypatel2020@gmail.com\n");
            }
            catch { }
        }

        private void kryptonButton2_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog fld = new OpenFileDialog();
                fld.Title = "Select PDF File";
                fld.Filter = "PDF File|*.pdf";
                fld.ShowDialog();
                if (fld.FileName != "")
                {
                    txt_inpdf.Text = fld.FileName;
                }
            }
            catch (Exception erd)
            {
                gCls.show_error(erd.Message.ToString());
                return;
            }
        }

        private void kryptonButton3_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog fld = new OpenFileDialog();
                fld.Title = "Select cover image file";
                fld.Filter = "JPG File|*.jpg";
                fld.ShowDialog();
                if (fld.FileName != "")
                {
                    txt_epubcover.Text = fld.FileName;
                }
            }
            catch (Exception erd)
            {
                gCls.show_error(erd.Message.ToString());
                return;
            }
        }

        private void kryptonButton4_Click(object sender, EventArgs e)
        {
            try
            {
                Application.Exit();
            }
            catch (Exception erd) {
                gCls.show_error(erd.Message.ToString());
                return;
            }
        }
    }
}
