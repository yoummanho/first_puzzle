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
 static Color ink = Color.FromArgb(30,30,36);
 static readonly Brush inkBrush = new SolidBrush(ink);
 static System.Drawing.Text.PrivateFontCollection fonts = new System.Drawing.Text.PrivateFontCollection();
 static Graphics G(Bitmap b) { var g=Graphics.FromImage(b); g.SmoothingMode=SmoothingMode.AntiAlias; g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit; return g; }
 static GraphicsPath Round(float x,float y,float w,float h,float r) { var p=new GraphicsPath(); p.AddArc(x,y,r,r,180,90); p.AddArc(x+w-r,y,r,r,270,90); p.AddArc(x+w-r,y+h-r,r,r,0,90); p.AddArc(x,y+h-r,r,r,90,90); p.CloseFigure(); return p; }
 static string Group(string name) { return name.StartsWith("icon_")?"Buttons/Icons":name.StartsWith("label_")?"Buttons/Labels":name=="background"?"Backgrounds":name=="logo"?"Brand":"Surfaces"; }
 static void Save(Bitmap b,string name) { string folder=Path.Combine(dir,Group(name)); Directory.CreateDirectory(folder); b.Save(Path.Combine(folder,name+".png"),ImageFormat.Png); b.Dispose(); }
 static void Text(Graphics g,string s,RectangleF rect,float size,Color color,bool outline=false) {
  using(var p=new GraphicsPath()) using(var sf=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap }) {
   // Build unrestricted outlines first; a bounded text layout can silently drop the last glyph.
   p.AddString(s,fonts.Families[0],(int)FontStyle.Regular,size,new PointF(0,0),StringFormat.GenericTypographic);
   var bounds=p.GetBounds();
   float scale=Math.Min(1f,Math.Min((rect.Width-16)/bounds.Width,(rect.Height-16)/bounds.Height));
   using(var fit=new Matrix(scale,0,0,scale,rect.X+rect.Width/2-scale*(bounds.X+bounds.Width/2),rect.Y+rect.Height/2-scale*(bounds.Y+bounds.Height/2)))p.Transform(fit);
   using(var shadow=(GraphicsPath)p.Clone()) using(var matrix=new Matrix()) using(var brush=new SolidBrush(ink)) using(var pen=new Pen(ink,outline?10:5){LineJoin=LineJoin.Round}) {
    matrix.Translate(0,outline?6:4); shadow.Transform(matrix); g.DrawPath(pen,shadow); g.FillPath(brush,shadow);
    g.DrawPath(pen,p);
   }
   using(var brush=new SolidBrush(outline?Color.FromArgb(255,190,11):Color.White))g.FillPath(brush,p);
  }
 }
 public static void Build(string output) {
  dir=output; Directory.CreateDirectory(dir);
  fonts.AddFontFile(Path.Combine(dir,"Fonts/LilitaOne-Regular.ttf"));
  foreach(string name in new[]{"panel","pill","tile"}) {
   var b=new Bitmap(160,160); using(var g=G(b)) {
    // Neutral fill mask. Runtime adds untinted 3px stroke and 6px hard shadow.
    using(var p=Round(0,0,160,160,name=="pill"?80:30)) g.FillPath(Brushes.White,p);
   } Save(b,name);
  }
  var circle=new Bitmap(256,256);using(var g=G(circle)){g.FillEllipse(Brushes.White,0,0,256,256);}Save(circle,"circle");
  var bg=new Bitmap(720,1280); using(var g=G(bg)) {
   g.Clear(Color.FromArgb(248,249,250));
   using(var pen=new Pen(Color.FromArgb(46,229,231,235),3){LineJoin=LineJoin.Round})
   for(int y=50,row=0;y<1280;y+=150,row++)for(int x=35;x<720;x+=150){
    int dx=x+(row%2)*45; int kind=(x/150+row)%3;
    if(kind==0){var points=new PointF[10];for(int i=0;i<10;i++){double a=-Math.PI/2+i*Math.PI/5;float r=i%2==0?22:10;points[i]=new PointF(dx+(float)Math.Cos(a)*r,y+(float)Math.Sin(a)*r);}g.DrawPolygon(pen,points);}
    else if(kind==1)g.DrawPolygon(pen,new[]{new Point(dx+5,y-22),new Point(dx-17,y+3),new Point(dx-2,y+3),new Point(dx-8,y+24),new Point(dx+19,y-5),new Point(dx+4,y-5)});
    else {using(var p=Round(dx-22,y-22,44,44,7)){g.DrawPath(pen,p);}g.DrawArc(pen,dx-8,y-30,16,16,180,180);}
   }
  } Save(bg,"background");
  var logo=new Bitmap(1000,400); using(var g=G(logo)) {Text(g,"PICTURE",new RectangleF(16,0,968,200),178,ink,true);Text(g,"PUZZLE",new RectangleF(16,185,968,200),178,ink,true);} Save(logo,"logo");
  foreach(var name in new[]{"pause","undo","preview","lock","prev","next","gallery","play","close","clock"}) {
   var b=new Bitmap(256,256); using(var g=G(b)) using(var pen=new Pen(ink,12){StartCap=LineCap.Round,EndCap=LineCap.Round,LineJoin=LineJoin.Round}) {
    if(name=="lock") {
     g.DrawArc(pen,74,22,108,138,180,180); using(var gold=new SolidBrush(Color.FromArgb(255,190,11)))using(var p=Round(52,100,152,132,24)){g.FillPath(gold,p);g.DrawPath(pen,p);} using(var brush=new SolidBrush(ink)){g.FillEllipse(brush,110,139,36,36);g.FillRectangle(brush,119,159,18,38);}
    } else if(name=="pause") {using(var pink=new SolidBrush(Color.FromArgb(255,51,102)))foreach(int x in new[]{74,144})using(var p=Round(x,54,38,148,18)){g.FillPath(pink,p);g.DrawPath(pen,p);}}
    else if(name=="preview") {using(var p=new GraphicsPath()){p.AddBezier(30,128,82,40,174,40,226,128);p.AddBezier(226,128,174,216,82,216,30,128);g.FillPath(Brushes.White,p);g.DrawPath(pen,p);}g.FillEllipse(inkBrush,89,88,78,78);g.FillEllipse(Brushes.White,108,97,23,23);}
    else if(name=="undo") {g.DrawArc(new Pen(ink,22){EndCap=LineCap.Round},72,92,122,110,210,265); g.FillPolygon(inkBrush,new[]{new Point(40,107),new Point(111,53),new Point(111,155)});}
    else if(name=="prev"||name=="next"||name=="play") {bool left=name=="prev";g.FillPolygon(inkBrush,new[]{new Point(left?75:181,128),new Point(left?172:84,63),new Point(left?172:84,193)});}
    else if(name=="close") {g.DrawLine(pen,70,70,186,186);g.DrawLine(pen,186,70,70,186);}
    else if(name=="clock") {g.DrawEllipse(pen,35,35,186,186);g.DrawLine(pen,128,128,128,70);g.DrawLine(pen,128,128,167,151);}
    else {g.DrawRectangle(pen,38,48,180,160);g.DrawEllipse(pen,68,72,32,32);g.DrawLines(pen,new[]{new Point(45,186),new Point(110,127),new Point(145,161),new Point(183,117),new Point(212,151)});}
   } Save(b,"icon_"+name);
  }
  string[] labels={"TAP TO START","COLLECTIBLE ILLUSTRATION\nSLIDING PUZZLE","Slide a tile into the empty space.","Gallery","View: PORTRAIT","View: LANDSCAPE","Play Stage","ALL","3x3","3x4","4x3","3x5","3x6","6x3","3x7","GAME","SYSTEM","Restart","Undo","X","Back to lobby","Back to Stage Lobby","Close Full View","Music: ON","Music: OFF","Sound: ON","Sound: OFF","Line slide: ON","Line slide: OFF","Animation: ON","Animation: OFF","Numbers: ON","Numbers: OFF","Number: ON","Number: OFF","Hold to preview","Next stage","Play this again","LOCKED","UNLOCKED","CLEARED","VIEW FULL ART","LOCKED (CLEAR STAGE)"};
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
