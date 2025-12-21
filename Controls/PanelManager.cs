using System;
using System.Windows;
using LayoutEditor.Models;

namespace LayoutEditor.Controls
{
    /// <summary>
    /// Manages floating panels (excluding LayersPanel which already exists)
    /// </summary>
    public class PanelManager
    {
        private MainWindow _mainWindow;
        
        public ToolboxPanel Toolbox { get; private set; }
        public PropertiesPanel Properties { get; private set; } = null!;
        public ExplorerPanel Explorer { get; private set; } = null!;
        public LayoutsPanel Layouts { get; private set; } = null!;
        
        // Note: LayersPanel is excluded - you already have one as a UserControl
        
        public PanelManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            
            // Create panels
            Toolbox = new ToolboxPanel();
            Explorer = new ExplorerPanel();
            Layouts = new LayoutsPanel();
            
            // Properties panel created on demand
            CreatePropertiesPanel();
            
            // Set ownership
            Toolbox.SetOwner(mainWindow);
            Explorer.SetOwner(mainWindow);
            Layouts.SetOwner(mainWindow);
        }
        
        private void CreatePropertiesPanel()
        {
            Properties = new PropertiesPanel();
            Properties.SetOwner(_mainWindow);
            Properties.ApplyRequested += () => _mainWindow.RefreshCanvas();
            Properties.DeleteRequested += () => _mainWindow.DeleteSelectedFromPanel();
            Properties.Closed += (s, e) => _propertiesClosed = true;
            _propertiesClosed = false;
        }
        
        /// <summary>
        /// Position panels around the main window
        /// </summary>
        public void PositionPanels()
        {
            // Toolbox - left side
            Toolbox.PositionRelativeTo(_mainWindow, HorizontalAlignment.Left, VerticalAlignment.Top, 10, 0);
            
            // Properties - left side, below toolbox
            Properties.Left = Toolbox.Left;
            Properties.Top = Toolbox.Top + Toolbox.Height + 10;
            
            // Explorer - right side
            Explorer.PositionRelativeTo(_mainWindow, HorizontalAlignment.Right, VerticalAlignment.Top, -10, 0);
            
            // Layouts - centered
            Layouts.PositionRelativeTo(_mainWindow, HorizontalAlignment.Center, VerticalAlignment.Center, 0, 0);
        }
        
        public void ShowAllPanels()
        {
            Toolbox.Show();
            Properties.Show();
            Explorer.Show();
        }
        
        public void HideAllPanels()
        {
            Toolbox.Hide();
            Properties.Hide();
            Explorer.Hide();
            Layouts.Hide();
        }
        
        public void TogglePanel(string panelName)
        {
            switch (panelName.ToLower())
            {
                case "toolbox":
                    Toolbox.ToggleVisibility();
                    break;
                case "properties":
                    Properties.ToggleVisibility();
                    break;
                case "explorer":
                    Explorer.ToggleVisibility();
                    break;
                case "layouts":
                    Layouts.ToggleVisibility();
                    break;
            }
        }
        
        public bool IsPanelVisible(string panelName)
        {
            return panelName.ToLower() switch
            {
                "toolbox" => Toolbox.IsVisible,
                "properties" => Properties.IsVisible,
                "explorer" => Explorer.IsVisible,
                "layouts" => Layouts.IsVisible,
                _ => false
            };
        }
        
        public void LoadLayout(LayoutData layout)
        {
            _currentLayout = layout;  // Store for panel recreation
            Explorer.LoadLayout(layout);
            Properties.SetLayout(layout);
        }
        
        public void RefreshAll()
        {
            Explorer.RefreshTree();
        }
        
        private bool _propertiesClosed = false;
        private LayoutData? _currentLayout;
        
        private void EnsurePropertiesPanel()
        {
            // If panel was closed, recreate it
            if (_propertiesClosed)
            {
                CreatePropertiesPanel();
                // Restore the layout reference
                if (_currentLayout != null)
                {
                    Properties.SetLayout(_currentLayout);
                }
            }
        }
        
        public void ShowNodeProperties(NodeData node)
        {
            EnsurePropertiesPanel();
            Properties.ShowNodeProperties(node);
            if (!Properties.IsVisible)
                Properties.Show();
            Properties.Activate();
        }
        
        public void ShowPathProperties(PathData path)
        {
            EnsurePropertiesPanel();
            Properties.ShowPathProperties(path);
            if (!Properties.IsVisible)
                Properties.Show();
            Properties.Activate();
        }
        
        public void ShowGroupProperties(GroupData group)
        {
            EnsurePropertiesPanel();
            Properties.ShowGroupProperties(group);
            if (!Properties.IsVisible)
                Properties.Show();
            Properties.Activate();
        }
        
        public void ShowWallProperties(WallData wall)
        {
            EnsurePropertiesPanel();
            Properties.ShowWallProperties(wall);
            if (!Properties.IsVisible)
                Properties.Show();
            Properties.Activate();
        }
        
        public void ClearSelection()
        {
            if (!_propertiesClosed)
                Properties?.ClearSelection();
        }
        
        public void ApplyVisibility(Models.EditorSettings settings)
        {
            // Floating panels - check if still valid before showing
            try
            {
                if (Toolbox != null && !Toolbox.IsClosed)
                {
                    if (settings.ShowToolbox) Toolbox.Show(); else Toolbox.Hide();
                }
                if (Explorer != null && !Explorer.IsClosed)
                {
                    if (settings.ShowExplorer) Explorer.Show(); else Explorer.Hide();
                }
            }
            catch (InvalidOperationException)
            {
                // Window was closed, ignore
            }
            // Properties shown on demand, not always visible
        }
        
        /// <summary>
        /// Force close all panels - call this when app is exiting
        /// </summary>
        public void Dispose()
        {
            Toolbox.ForceClose();
            Properties.ForceClose();
            Explorer.ForceClose();
            Layouts.ForceClose();
        }
    }
}
