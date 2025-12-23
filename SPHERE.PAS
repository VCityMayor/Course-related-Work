program BallSector;

uses
  Crt, Dos;

const
  VESA_MODE = $100;
  SCREEN_WIDTH = 640;
  SCREEN_HEIGHT = 400;
  CENTER_X = 320;
  CENTER_Y = 200;
  SEG_VIDEO = $A000;
  SPHERE_RADIUS = 100;
  SECTOR_ANGLE = 90;
  POINTS_COUNT = 36;

type
  Point3D = record
    x, y, z: real;
  end;
  
  Point2D = record
    x, y: integer;
  end;

var
  SpherePoints: array[1..POINTS_COUNT] of Point3D;
  ScreenPoints: array[1..POINTS_COUNT] of Point2D;
  Angle: real;

procedure SetVideoMode(mode: word); assembler;
asm
  mov ax, 4F02h
  mov bx, mode
  int 10h
end;

procedure PutPixel(x, y: integer; color: byte);
var
  video_offset: word;
begin
  if (x >= 0) and (x < SCREEN_WIDTH) and (y >= 0) and (y < SCREEN_HEIGHT) then
  begin
    video_offset := y * SCREEN_WIDTH + x;
    asm
      mov ax, SEG_VIDEO
      mov es, ax
      mov di, video_offset
      mov al, color
      mov es:[di], al
    end;
  end;
end;

procedure DrawLine(x1, y1, x2, y2: integer; color: byte);
var
  dx, dy, sx, sy, err, e2: integer;
begin
  dx := abs(x2 - x1);
  dy := abs(y2 - y1);
  
  if x1 < x2 then sx := 1 else sx := -1;
  if y1 < y2 then sy := 1 else sy := -1;
  
  err := dx - dy;
  
  while (x1 <> x2) or (y1 <> y2) do
  begin
    PutPixel(x1, y1, color);
    e2 := 2 * err;
    if e2 > -dy then
    begin
      err := err - dy;
      x1 := x1 + sx;
    end;
    if e2 < dx then
    begin
      err := err + dx;
      y1 := y1 + sy;
    end;
  end;
  PutPixel(x2, y2, color);
end;

procedure ClearScreen;
begin
  FillChar(Mem[$A000:0000], 64000, 0);
end;

procedure InitSphere;
var
  i, j, idx: integer;
  phi, theta: real;
  maxTheta: real;
  rings, pointsPerRing: integer;
begin
  rings := 6;
  pointsPerRing := POINTS_COUNT div rings;
  maxTheta := Pi * SECTOR_ANGLE / 180;
  
  idx := 1;
  for j := 0 to rings - 1 do
  begin
    theta := maxTheta * j / (rings - 1);
    for i := 0 to pointsPerRing - 1 do
    begin
      phi := 2 * Pi * i / pointsPerRing;
      
      SpherePoints[idx].x := SPHERE_RADIUS * sin(theta) * cos(phi);
      SpherePoints[idx].y := SPHERE_RADIUS * sin(theta) * sin(phi);
      SpherePoints[idx].z := SPHERE_RADIUS * cos(theta);
      
      idx := idx + 1;
      if idx > POINTS_COUNT then exit;
    end;
  end;
end;

procedure RotatePoint(var p: Point3D; angle: real);
var
  tempX, tempY: real;
  cosA, sinA: real;
begin
  cosA := cos(angle);
  sinA := sin(angle);
  
  tempX := p.x * cosA - p.y * sinA;
  tempY := p.x * sinA + p.y * cosA;
  
  p.x := tempX;
  p.y := tempY;
  
  tempX := p.x * 0.7071 - p.z * 0.7071;
  tempY := p.x * 0.7071 + p.z * 0.7071;
  
  p.x := tempX;
  p.z := tempY;
end;

procedure ProjectPoint(p3D: Point3D; var x2D, y2D: integer);
var
  scale: real;
begin
  scale := 500 / (500 + p3D.z);
  x2D := CENTER_X + round(p3D.x * scale);
  y2D := CENTER_Y - round(p3D.y * scale);
end;

procedure DrawSphere;
var
  i, j, idx1, idx2: integer;
  rings, pointsPerRing: integer;
begin
  for i := 1 to POINTS_COUNT do
  begin
    RotatePoint(SpherePoints[i], Angle);
    ProjectPoint(SpherePoints[i], ScreenPoints[i].x, ScreenPoints[i].y);
  end;
  
  rings := 6;
  pointsPerRing := POINTS_COUNT div rings;
  
  for j := 0 to rings - 1 do
  begin
    for i := 0 to pointsPerRing - 1 do
    begin
      idx1 := j * pointsPerRing + i + 1;
      idx2 := j * pointsPerRing + ((i + 1) mod pointsPerRing) + 1;
      
      if idx1 <= POINTS_COUNT then
        PutPixel(ScreenPoints[idx1].x, ScreenPoints[idx1].y, 14);
      
      if (idx1 <= POINTS_COUNT) and (idx2 <= POINTS_COUNT) then
        DrawLine(ScreenPoints[idx1].x, ScreenPoints[idx1].y,
                 ScreenPoints[idx2].x, ScreenPoints[idx2].y, 10);
    end;
  end;
  
  for i := 1 to pointsPerRing do
  begin
    for j := 0 to rings - 2 do
    begin
      idx1 := j * pointsPerRing + i;
      idx2 := (j + 1) * pointsPerRing + i;
      
      if (idx1 <= POINTS_COUNT) and (idx2 <= POINTS_COUNT) then
        DrawLine(ScreenPoints[idx1].x, ScreenPoints[idx1].y,
                 ScreenPoints[idx2].x, ScreenPoints[idx2].y, 9);
    end;
  end;
end;

begin
  InitSphere;
  Angle := 0;
  
  SetVideoMode(VESA_MODE);
  Delay(500);
  
  repeat
    ClearScreen;
    DrawSphere;
    
    Angle := Angle + Pi / 12;
    if Angle > 2 * Pi then
      Angle := Angle - 2 * Pi;
    
    Delay(100);
  until KeyPressed;
  
  SetVideoMode(3);
  writeln('Lense Completed!');
end.