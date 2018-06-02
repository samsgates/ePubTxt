using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Updf
/// </summary>
public class Updf
{

    public string b_rootpath { get; set; }
    public string b_outpath { get; set; }

    public string b_filenamewoe { get; set; }
    public string b_title { get; set; }
    public string b_author { get; set; }
    public string b_pdffile { get; set; }
    public string b_coverpath { get; set; }
   
  
  

    public Updf(string i_filenamewoe, string i_title, string i_author, string i_pdffile,string i_rootpath,string i_outpath,string i_coverpath)
    {
        try
        {


            b_filenamewoe = i_filenamewoe;
            b_title = i_title;
            b_author = i_author;
            b_pdffile = i_pdffile;           
            b_rootpath = i_rootpath;
            b_outpath = i_outpath;
            b_coverpath = i_coverpath;
           
        }
        catch { }
    }
}

public class tInfo
{
    public string p_left { get; set; }
    public string p_top { get; set; }
    public string p_text { get; set; }
    public string p_fontid { get; set; }

    public tInfo(string left, string top, string text, string fontid)
    {
        p_left = left;
        p_top = top;
        p_text = text;
        p_fontid = fontid;
    }


}

public class fInfo
{
    public string f_fontsize { get; set; }
    public string f_fontfamily { get; set; }
    public string f_color { get; set; }
    public string f_fontid { get; set; }


    public fInfo(string fontsize, string fontfamily, string color, string fontid)
    {
        f_fontsize = fontsize;
        f_fontfamily = fontfamily;
        f_color = color;
        f_fontid = fontid;

    }


}