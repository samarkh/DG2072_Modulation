# Save as FixMainWindowXaml.ps1 and run in your project directory

Write-Host "Fixing MainWindow.xaml..." -ForegroundColor Yellow

# Step 1: Create MainWindow.xaml from the txt file
$sourceFile = "Documents\MainWindow.xaml.txt"
$targetFile = "MainWindow.xaml"

if (Test-Path $sourceFile) {
    Write-Host "Found source file: $sourceFile" -ForegroundColor Green
    
    # Read content
    $content = Get-Content $sourceFile -Raw
    
    # Ensure XML declaration is present
    if (-not $content.StartsWith('<?xml')) {
        $content = '<?xml version="1.0" encoding="utf-8"?>' + "`r`n" + $content
    }
    
    # Save with UTF-8 encoding (no BOM)
    $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
    [System.IO.File]::WriteAllText($targetFile, $content, $Utf8NoBomEncoding)
    
    Write-Host "✓ Created $targetFile with proper encoding" -ForegroundColor Green
} else {
    Write-Host "✗ Source file not found: $sourceFile" -ForegroundColor Red
    exit
}

# Step 2: Update the .csproj file
$csprojFile = Get-ChildItem -Filter "*.csproj" | Select-Object -First 1

if ($csprojFile) {
    Write-Host "`nUpdating project file: $($csprojFile.Name)" -ForegroundColor Cyan
    
    $csprojContent = Get-Content $csprojFile.FullName -Raw
    
    # Check if MainWindow.xaml is already included
    if ($csprojContent -notmatch '<Page Include="MainWindow\.xaml"') {
        Write-Host "Adding MainWindow.xaml to project file..." -ForegroundColor Yellow
        
        # Find the right place to insert (after other Page items or in an ItemGroup)
        if ($csprojContent -match '(<ItemGroup>[\s\S]*?<Page Include=.*?</ItemGroup>)') {
            # Insert before the closing ItemGroup
            $insertPoint = $csprojContent.LastIndexOf('</ItemGroup>', $matches[0].Index + $matches[0].Length)
            $pageEntry = @"
    <Page Include="MainWindow.xaml">
      <Generator>MSBuild:Compile</Generator>
      <SubType>Designer</SubType>
    </Page>
"@
            $csprojContent = $csprojContent.Insert($insertPoint, "`r`n$pageEntry`r`n  ")
        } else {
            # Create a new ItemGroup
            $insertPoint = $csprojContent.LastIndexOf('</Project>')
            $itemGroup = @"
  <ItemGroup>
    <Page Include="MainWindow.xaml">
      <Generator>MSBuild:Compile</Generator>
      <SubType>Designer</SubType>
    </Page>
  </ItemGroup>
"@
            $csprojContent = $csprojContent.Insert($insertPoint, "`r`n$itemGroup`r`n")
        }
        
        # Save the updated project file
        Set-Content -Path $csprojFile.FullName -Value $csprojContent -Encoding UTF8
        Write-Host "✓ Updated project file" -ForegroundColor Green
    } else {
        Write-Host "✓ MainWindow.xaml already in project file" -ForegroundColor Green
    }
}

# Step 3: Add the Modulation buttons to the XAML
Write-Host "`nAdding Modulation buttons to XAML..." -ForegroundColor Cyan

$xamlContent = Get-Content $targetFile -Raw

# Check if buttons already exist
if ($xamlContent -notmatch 'ApplyModulationButton') {
    # Find the Modulation tab and add buttons
    $modulationTabPattern = '(<TabItem Header="Modulation"[^>]*>[\s\S]*?<Grid[^>]*>)([\s\S]*?)(</Grid>[\s\S]*?</TabItem>)'
    
    if ($xamlContent -match $modulationTabPattern) {
        $gridContent = $matches[2]
        
        # Check if Grid.RowDefinitions exist
        if ($gridContent -notmatch '<Grid.RowDefinitions>') {
            # Add Grid.RowDefinitions and buttons
            $newGridContent = @"
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Existing GroupBox stays in Row 0 -->
        <GroupBox Header="Modulation Settings" Margin="10" Padding="10" Grid.Row="0">
"@
            $gridContent = $gridContent -replace '(<GroupBox Header="Modulation Settings"[^>]*>)', $newGridContent
            
            # Add buttons before closing Grid tag
            $buttons = @"
        
        <!-- Apply/Disable buttons in Row 1 -->
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Center" 
                    Grid.Row="1" 
                    Margin="0,10">
            <Button x:Name="ApplyModulationButton" 
                    Content="Apply Modulation" 
                    Width="120" 
                    Height="30" 
                    Margin="5"
                    Click="ApplyModulationButton_Click"
                    Background="#FF4CAF50"
                    Foreground="White"
                    ToolTip="Apply the current modulation settings to the signal"/>
            
            <Button x:Name="DisableModulationButton" 
                    Content="Disable Modulation" 
                    Width="120" 
                    Height="30" 
                    Margin="5"
                    Click="DisableModulationButton_Click"
                    Background="#FFF44336"
                    Foreground="White"
                    ToolTip="Turn off all modulation"/>
        </StackPanel>
"@
            $gridContent = $gridContent -replace '([\s\S]*)(</Grid>)', "`$1$buttons`r`n    `$2"
            
            # Replace the entire match
            $xamlContent = $xamlContent -replace $modulationTabPattern, "`$1$gridContent`$3"
            
            # Save the updated XAML
            [System.IO.File]::WriteAllText($targetFile, $xamlContent, $Utf8NoBomEncoding)
            Write-Host "✓ Added Modulation buttons to XAML" -ForegroundColor Green
        }
    } else {
        Write-Host "✗ Could not find Modulation tab pattern" -ForegroundColor Red
    }
} else {
    Write-Host "✓ Modulation buttons already exist" -ForegroundColor Green
}

Write-Host "`nFix completed!" -ForegroundColor Green
Write-Host "Please:" -ForegroundColor Yellow
Write-Host "1. Open Visual Studio" -ForegroundColor White
Write-Host "2. Clean and rebuild the solution" -ForegroundColor White
Write-Host "3. MainWindow.xaml should now be visible and functional" -ForegroundColor White