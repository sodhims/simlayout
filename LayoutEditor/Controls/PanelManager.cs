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
        public PropertiesPanel Properties { get; private set; }
        public ExplorerPanel Explorer { get; private set; }
        public LayoutsPanel Layouts { get; private set; }
        
        // Note: LayersPanel is excluded - you already have one as a UserControl
        
        public PanelManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            
            // Create panels
            Toolbox = new ToolboxPanel();
            Properties = new PropertiesPanel();
            Explorer = new ExplorerPanel();
            Layouts = new LayoutsPanel();
            
            // Set ownership
            Toolbox.SetOwner(mainWindow);
            Properties.SetOwner(mainWindow);
            Explorer.SetOwner(mainWindow);
            Layouts.SetOwner(mainWindow);
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
            Explorer.LoadLayout(layout);
        }
        
        public void RefreshAll()
        {
            Explorer.RefreshTree();
        }
        
        public void ShowNodeProperties(NodeData node)
        {
            Properties.ShowNodeProperties(node);
            if (!Properties.IsVisible)
                Properties.Show();
        }
        
        public void ClearSelection()
        {
            Properties.ClearSelection();
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
