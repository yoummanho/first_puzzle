$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
public static class ConceptAssets {
 static string dir;
 static Color ink = Color.FromArgb(18,18,16);
 static Graphics G(Bitmap b) { var g=Graphics.FromImage(b); g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; return g; }
 static GraphicsPath Round(float x,float y,float w,float h,float r) { var p=new GraphicsPath(); p.AddArc(x,y,r,r,180,90); p.AddArc(x+w-r,y,r,r,270,90); p.AddArc(x+w-r,y+h-r,r,r,0,90); p.AddArc(x,y+h-r,r,r,90,90); p.CloseFigure(); return p; }
 static void Save(Bitmap b,string name) { b.Save(Path.Combine(dir,name+".png"),ImageFormat.Png); b.Dispose(); }
 static void Text(Graphics g,string s,RectangleF rect,float size,Color color,bool outline=false) {
  using(var p=new GraphicsPath()) using(var family=new FontFamily("Arial")) using(var sf=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center }) {
   p.AddString(s,family,(int)FontStyle.Bold,size,rect,sf);
   if(outline) { using(var pen=new Pen(ink,14){LineJoin=LineJoin.Round}) g.DrawPath(pen,p); using(var brush=new LinearGradientBrush(rect,Color.FromArgb(255,223,36),Color.FromArgb(255,130,10),90)) g.FillPath(brush,p); }
   else using(var brush=new SolidBrush(color)) g.FillPath(brush,p);
  }
 }
 public static void Build(string output) {
  dir=output; Directory.CreateDirectory(dir);
  foreach(string name in new[]{"panel","pill","tile"}) {
   var b=new Bitmap(160,160); using(var g=G(b)) {
    using(var p=Round(5,12,150,143,name=="pill"?75:30)) g.FillPath(Brushes.Black,p);
    using(var p=Round(5,5,150,143,name=="pill"?75:30)) {g.FillPath(Brushes.White,p); using(var pen=new Pen(ink,6))g.DrawPath(pen,p);}
   } Save(b,name);
  }
  var circle=new Bitmap(256,256);using(var g=G(circle)){g.FillEllipse(Brushes.Black,5,16,246,235);g.FillEllipse(Brushes.White,5,5,246,235);using(var pen=new Pen(ink,8))g.DrawEllipse(pen,5,5,246,235);}Save(circle,"circle");
  var bg=new Bitmap(720,1280); using(var g=G(bg)) {g.Clear(Color.FromArgb(250,250,243));} Save(bg,"background");
  var logo=new Bitmap(1000,400); using(var g=G(logo)) {Text(g,"PICTURE",new RectangleF(16,0,968,200),178,ink,true);Text(g,"PUZZLE",new RectangleF(16,185,968,200),178,ink,true);} Save(logo,"logo");
  foreach(var name in new[]{"pause","undo","preview","lock","prev","next","gallery","play","close","clock"}) {
   var b=new Bitmap(256,256); using(var g=G(b)) using(var pen=new Pen(ink,12){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}) {
    if(name=="lock") {
     g.DrawArc(pen,74,22,108,138,180,180); using(var gold=new SolidBrush(Color.FromArgb(255,210,25)))using(var p=Round(52,100,152,132,24)){g.FillPath(gold,p);g.DrawPath(pen,p);} g.FillEllipse(Brushes.Black,110,139,36,36);g.FillRectangle(Brushes.Black,119,159,18,38);
    } else if(name=="pause") {using(var pink=new SolidBrush(Color.FromArgb(245,81,138)))foreach(int x in new[]{74,144})using(var p=Round(x,54,38,148,18)){g.FillPath(pink,p);g.DrawPath(pen,p);}}
    else if(name=="preview") {using(var p=new GraphicsPath()){p.AddBezier(30,128,82,40,174,40,226,128);p.AddBezier(226,128,174,216,82,216,30,128);g.FillPath(Brushes.White,p);g.DrawPath(pen,p);}g.FillEllipse(Brushes.Black,89,88,78,78);g.FillEllipse(Brushes.White,108,97,23,23);}
    else if(name=="undo") {g.DrawArc(new Pen(ink,22){EndCap=LineCap.Round},72,92,122,110,210,265); g.FillPolygon(Brushes.Black,new[]{new Point(40,107),new Point(111,53),new Point(111,155)});}
    else if(name=="prev"||name=="next"||name=="play") {bool left=name=="prev";g.FillPolygon(Brushes.Black,new[]{new Point(left?75:181,128),new Point(left?172:84,63),new Point(left?172:84,193)});}
    else if(name=="close") {g.DrawLine(pen,70,70,186,186);g.DrawLine(pen,186,70,70,186);}
    else if(name=="clock") {g.DrawEllipse(pen,35,35,186,186);g.DrawLine(pen,128,128,128,70);g.DrawLine(pen,128,128,167,151);}
    else {g.DrawRectangle(pen,38,48,180,160);g.DrawEllipse(pen,68,72,32,32);g.DrawLines(pen,new[]{new Point(45,186),new Point(110,127),new Point(145,161),new Point(183,117),new Point(212,151)});}
   } Save(b,"icon_"+name);
  }
  string[] labels={"TAP TO START","COLLECTIBLE ILLUSTRATION\nSLIDING PUZZLE","Slide a tile into the empty space.","Gallery","View: PORTRAIT","View: LANDSCAPE","Play Stage","ALL","3x3","3x4","4x3","3x5","3x6","6x3","3x7","GAME","SYSTEM","Restart","Back to lobby","Back to Stage Lobby","Close Full View","Music: ON","Music: OFF","Sound: ON","Sound: OFF","Line slide: ON","Line slide: OFF","Animation: ON","Animation: OFF","Numbers: ON","Numbers: OFF","Number: ON","Number: OFF","Hold to preview","Next stage","Play this again","LOCKED","UNLOCKED","CLEARED","VIEW FULL ART","LOCKED (CLEAR STAGE)"};
  foreach(string s in labels) {
   bool white=s.StartsWith("COLLECTIBLE")||s.StartsWith("Slide a tile");
   bool small=s.Length<5; int longest=0;foreach(var line in s.Split('\n'))longest=Math.Max(longest,line.Length);
   int w=small?256:Math.Max(220,longest*40+32); int h=s.StartsWith("COLLECTIBLE")?200:128;
   var b=new Bitmap(w,h); using(var g=G(b))Text(g,s,new RectangleF(8,4,w-16,h-8),small?76:(s.Length>28?48:64),white?Color.White:ink);
   Save(b,"label_"+Regex.Replace(s.ToLowerInvariant(),"[^a-z0-9]+","_").Trim('_'));
  }
 }
}
'@
[ConceptAssets]::Build((Join-Path $PSScriptRoot '../Assets/Resources/ConceptUI'))
