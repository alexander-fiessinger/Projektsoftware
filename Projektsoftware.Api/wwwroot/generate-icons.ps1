# PowerShell-Skript zum Generieren der PWA-Icons mit Corporate Identity
# Erstellt icon-192.png und icon-512.png mit professionellem Projekt-Icon

Add-Type -AssemblyName System.Drawing

function Create-Icon($size, $outputPath) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Corporate Identity Gradient: #1B365D → #2E5090
    $color1 = [System.Drawing.Color]::FromArgb(27, 54, 93)
    $color2 = [System.Drawing.Color]::FromArgb(46, 80, 144)
    $gold = [System.Drawing.Color]::FromArgb(200, 162, 81)
    $white = [System.Drawing.Color]::White
    $green = [System.Drawing.Color]::FromArgb(52, 211, 153)

    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect, $color1, $color2, 45)
    $g.FillRectangle($brush, $rect)

    # Projekt-Card in der Mitte
    $cardMargin = $size * 0.2
    $cardWidth = $size * 0.6
    $cardHeight = $size * 0.7
    $cardX = $cardMargin
    $cardY = $size * 0.15

    # Card Background (weiß)
    $cardRect = New-Object System.Drawing.RectangleF($cardX, $cardY, $cardWidth, $cardHeight)
    $cardPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $radius = $size * 0.08
    $cardPath.AddArc($cardX, $cardY, $radius, $radius, 180, 90)
    $cardPath.AddArc($cardX + $cardWidth - $radius, $cardY, $radius, $radius, 270, 90)
    $cardPath.AddArc($cardX + $cardWidth - $radius, $cardY + $cardHeight - $radius, $radius, $radius, 0, 90)
    $cardPath.AddArc($cardX, $cardY + $cardHeight - $radius, $radius, $radius, 90, 90)
    $cardPath.CloseFigure()

    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(250, 255, 255, 255))
    $g.FillPath($whiteBrush, $cardPath)

    # Gold Header
    $headerHeight = $size * 0.1
    $headerPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $headerPath.AddArc($cardX, $cardY, $radius, $radius, 180, 90)
    $headerPath.AddArc($cardX + $cardWidth - $radius, $cardY, $radius, $radius, 270, 90)
    $headerPath.AddLine($cardX + $cardWidth, $cardY + $headerHeight, $cardX, $cardY + $headerHeight)
    $headerPath.CloseFigure()

    $goldBrush = New-Object System.Drawing.SolidBrush($gold)
    $g.FillPath($goldBrush, $headerPath)

    # Task-Checkmarks (3 Zeilen)
    $taskStartY = $cardY + $headerHeight + ($size * 0.1)
    $taskSpacing = $size * 0.12
    $checkSize = $size * 0.04
    $lineWidth = $cardWidth * 0.6
    $lineHeight = $size * 0.025

    for($i = 0; $i -lt 3; $i++) {
        $y = $taskStartY + ($i * $taskSpacing)
        $checkX = $cardX + ($size * 0.08)

        # Checkmark-Kreis
        if($i -lt 2) {
            $checkBrush = New-Object System.Drawing.SolidBrush($green)
        } else {
            $checkBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(100, 227, 242, 253))
        }
        $g.FillEllipse($checkBrush, $checkX - $checkSize/2, $y - $checkSize/2, $checkSize, $checkSize)

        # Task-Linie
        $lineX = $checkX + $checkSize + ($size * 0.03)
        $lineRect = New-Object System.Drawing.RectangleF($lineX, $y - $lineHeight/2, $lineWidth * (1 - $i*0.15), $lineHeight)
        $linePath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $lineRadius = $lineHeight / 2
        $linePath.AddArc($lineRect.X, $lineRect.Y, $lineRadius*2, $lineRadius*2, 180, 90)
        $linePath.AddArc($lineRect.X + $lineRect.Width - $lineRadius*2, $lineRect.Y, $lineRadius*2, $lineRadius*2, 270, 90)
        $linePath.AddArc($lineRect.X + $lineRect.Width - $lineRadius*2, $lineRect.Y + $lineRect.Height - $lineRadius*2, $lineRadius*2, $lineRadius*2, 0, 90)
        $linePath.AddArc($lineRect.X, $lineRect.Y + $lineRect.Height - $lineRadius*2, $lineRadius*2, $lineRadius*2, 90, 90)
        $linePath.CloseFigure()

        $lineBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(180, 227, 242, 253))
        $g.FillPath($lineBrush, $linePath)
    }

    $bmp.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose()
    $bmp.Dispose()
}

Write-Host "Generiere professionelle PWA-Icons..." -ForegroundColor Cyan

Create-Icon 192 "icon-192.png"
Write-Host "✓ icon-192.png erstellt" -ForegroundColor Green

Create-Icon 512 "icon-512.png"
Write-Host "✓ icon-512.png erstellt" -ForegroundColor Green

Write-Host "Fertig! Professionelles Projekt-Icon mit Corporate Identity" -ForegroundColor Green
