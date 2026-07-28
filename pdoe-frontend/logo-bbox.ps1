Add-Type -AssemblyName System.Drawing
$path = "J:\Stage AFB_DSI_CI\pdoe-fourth\pdoe-fourth\pdoe-frontend\public\logo-afriland-first-bank.png"
$bmp = New-Object System.Drawing.Bitmap $path
$w = $bmp.Width
$h = $bmp.Height
$rect = New-Object System.Drawing.Rectangle 0,0,$w,$h
$bd = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$bytes = New-Object byte[] ($bd.Stride * $h)
[System.Runtime.InteropServices.Marshal]::Copy($bd.Scan0, $bytes, 0, $bytes.Length)
$bmp.UnlockBits($bd)
$minX = $w
$minY = $h
$maxX = -1
$maxY = -1
for ($y = 0; $y -lt $h; $y++) {
  $rowOffset = $y * $bd.Stride
  for ($x = 0; $x -lt $w; $x++) {
    $i = $rowOffset + $x * 4
    $b = $bytes[$i]
    $g = $bytes[$i + 1]
    $r = $bytes[$i + 2]
    $a = $bytes[$i + 3]
    if ($a -gt 10 -and ($r -lt 245 -or $g -lt 245 -or $b -lt 245)) {
      if ($x -lt $minX) { $minX = $x }
      if ($x -gt $maxX) { $maxX = $x }
      if ($y -lt $minY) { $minY = $y }
      if ($y -gt $maxY) { $maxY = $y }
    }
  }
}
Write-Output "minX=$minX minY=$minY maxX=$maxX maxY=$maxY width=$w height=$h contentW=$($maxX-$minX) contentH=$($maxY-$minY)"
