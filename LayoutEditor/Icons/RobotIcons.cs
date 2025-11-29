using System.Collections.Generic;

namespace LayoutEditor.Icons
{
    /// <summary>
    /// Robot and automation icons
    /// </summary>
    public static class RobotIcons
    {
        public static Dictionary<string, IconDef> All => new()
        {
            // Industrial Robots
            ["robot_arm"] = new("Robot Arm", "M10,20 L14,20 L14,16 L10,16 Z M12,16 L12,12 L8,8 L8,4 M8,4 L6,2 M8,4 L10,2 M12,12 A2,2 0 1,1 12,8", "#9B59B6"),
            ["robot_6axis"] = new("6-Axis Robot", "M10,20 L14,20 L14,16 L10,16 Z M12,16 L12,12 M12,12 L8,8 M8,8 L12,4 M12,4 L16,6 M16,6 L18,4 M16,6 L18,8 M12,12 A2,2 0 1,0 14,12", "#9B59B6"),
            ["robot_scara"] = new("SCARA Robot", "M10,18 L14,18 L14,14 L10,14 Z M12,14 L12,10 M12,10 L18,10 M18,10 L18,6 M18,6 L22,6 M18,6 L18,2 M12,10 A2,2 0 1,0 14,10", "#9B59B6"),
            ["robot_delta"] = new("Delta Robot", "M8,4 L16,4 M4,4 L12,16 M20,4 L12,16 M8,4 L4,4 M16,4 L20,4 M10,16 L14,16 L14,20 L10,20 Z M12,16 L12,12", "#9B59B6"),
            ["robot_cartesian"] = new("Cartesian Robot", "M4,4 L20,4 M4,4 L4,12 M4,12 L12,12 M12,12 L12,20 M10,20 L14,20 L14,16 L10,16 M6,4 L6,8 M18,4 L18,8", "#9B59B6"),
            ["robot_cylindrical"] = new("Cylindrical Robot", "M8,20 L16,20 L16,16 L8,16 Z M12,16 L12,8 M12,8 L18,8 M18,8 L18,4 L20,4 M12,4 A4,4 0 1,0 12,12 M12,8 A1,1 0 1,0 13,8", "#9B59B6"),
            ["robot_collaborative"] = new("Cobot", "M10,20 L14,20 L14,16 L10,16 Z M12,16 L12,10 M12,10 L8,6 M8,6 L6,6 A2,2 0 1,0 6,10 M12,10 L16,6 M16,6 L18,6 A2,2 0 1,1 18,10 M12,10 A2,2 0 1,0 14,10", "#2ECC71"),
            ["robot_palletizing"] = new("Palletizing Robot", "M8,20 L16,20 L16,16 L8,16 Z M12,16 L12,8 M8,4 L16,4 L18,8 L6,8 Z M10,8 L10,12 L14,12 L14,8 M6,8 L6,12 L8,12 L8,8 M16,8 L16,12 L18,12 L18,8", "#9B59B6"),
            
            // End Effectors
            ["gripper_2finger"] = new("2-Finger Gripper", "M8,4 L8,12 L6,14 L6,20 M16,4 L16,12 L18,14 L18,20 M8,8 L16,8 M10,4 L14,4 L14,8 L10,8 Z", "#7F8C8D"),
            ["gripper_3finger"] = new("3-Finger Gripper", "M12,4 L12,8 M8,10 L6,16 M16,10 L18,16 M12,10 L12,16 M8,10 L16,10 M10,4 L14,4 L14,8 L10,8 Z", "#7F8C8D"),
            ["gripper_vacuum"] = new("Vacuum Gripper", "M8,4 L16,4 L16,8 L8,8 Z M10,8 L10,12 M14,8 L14,12 M8,12 A4,4 0 0,0 16,12 M12,12 L12,16 A2,2 0 1,0 12,20", "#7F8C8D"),
            ["gripper_magnetic"] = new("Magnetic Gripper", "M8,4 L16,4 L16,10 L8,10 Z M10,10 L10,14 L8,14 L8,18 L10,18 M14,10 L14,14 L16,14 L16,18 L14,18 M10,14 L14,14", "#E74C3C"),
            ["gripper_soft"] = new("Soft Gripper", "M10,4 L14,4 L14,8 L10,8 Z M8,8 Q6,12 8,16 L8,20 M16,8 Q18,12 16,16 L16,20 M10,8 L8,8 M14,8 L16,8", "#3498DB"),
            ["tool_changer"] = new("Tool Changer", "M8,2 L16,2 L16,6 L8,6 Z M10,6 L10,10 L14,10 L14,6 M8,10 L16,10 L16,14 L8,14 Z M10,14 L10,18 L8,18 L8,22 M14,14 L14,18 L16,18 L16,22", "#7F8C8D"),
            ["welding_torch"] = new("Welding Torch", "M10,2 L14,2 L14,8 L10,8 Z M12,8 L12,14 M10,14 L14,14 L13,20 L11,20 Z M8,18 L10,16 M14,16 L16,18", "#E67E22"),
            ["spray_gun"] = new("Spray Gun", "M8,4 L14,4 L14,10 L8,10 Z M14,6 L18,6 L18,8 L14,8 M11,10 L11,14 M9,14 L13,14 L12,20 L10,20 Z M8,16 L6,18 M14,16 L16,18", "#3498DB"),
            
            // Vision & Sensors
            ["camera_vision"] = new("Vision Camera", "M4,8 L18,8 L20,10 L20,16 L18,18 L4,18 L2,16 L2,10 Z M8,10 A3,3 0 1,0 8,16 A3,3 0 1,0 8,10 M14,10 L18,10 M14,14 L18,14", "#3498DB"),
            ["camera_3d"] = new("3D Camera", "M4,6 L20,6 L20,18 L4,18 Z M6,8 A2,2 0 1,0 6,12 M12,8 A2,2 0 1,0 12,12 M18,8 A2,2 0 1,0 18,12 M8,14 L16,14", "#3498DB"),
            ["sensor_proximity"] = new("Proximity Sensor", "M8,4 L16,4 L16,16 L8,16 Z M10,8 L14,8 L14,12 L10,12 Z M12,16 L12,20 M10,20 L14,20", "#2ECC71"),
            ["sensor_laser"] = new("Laser Sensor", "M6,6 L18,6 L18,14 L6,14 Z M10,10 L10,12 M14,8 L14,12 M6,10 L2,10 M2,8 L2,12 M18,10 L22,10", "#E74C3C"),
            ["scanner_barcode"] = new("Barcode Scanner", "M4,6 L20,6 L20,18 L4,18 Z M6,8 L6,16 M8,8 L8,16 M11,8 L11,16 M14,8 L14,16 M16,8 L16,16 M18,8 L18,16", "#7F8C8D"),
            ["scanner_rfid"] = new("RFID Reader", "M6,8 L18,8 L18,16 L6,16 Z M4,10 A8,8 0 0,0 4,14 M2,8 A10,10 0 0,0 2,16 M20,10 A8,8 0 0,1 20,14 M22,8 A10,10 0 0,1 22,16", "#9B59B6"),
            
            // Controllers
            ["plc"] = new("PLC", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 L18,10 L6,10 Z M6,12 L8,12 M6,14 L8,14 M6,16 L8,16 M10,12 L12,12 M10,14 L12,14 M10,16 L12,16 M14,12 A1,1 0 1,0 16,12 M14,16 A1,1 0 1,0 16,16", "#4A90D9"),
            ["hmi"] = new("HMI Panel", "M4,4 L20,4 L20,20 L4,20 Z M6,6 L18,6 L18,14 L6,14 Z M8,16 L10,16 L10,18 L8,18 M12,16 L14,16 L14,18 L12,18 M16,16 L18,16 L18,18 L16,18", "#4A90D9"),
            ["servo_drive"] = new("Servo Drive", "M6,4 L18,4 L18,20 L6,20 Z M8,6 L16,6 L16,10 L8,10 Z M8,12 L10,12 M8,14 L10,14 M12,12 A1,1 0 1,0 14,12 M8,16 L16,16 L16,18 L8,18", "#4A90D9"),
            ["robot_controller"] = new("Robot Controller", "M4,6 L20,6 L20,18 L4,18 Z M6,8 L18,8 L18,12 L6,12 Z M6,14 L8,14 M6,16 L8,16 M10,14 L12,14 M10,16 L12,16 M14,14 A1,1 0 1,0 16,14 M18,14 A1,1 0 1,0 20,14", "#9B59B6"),
        };
    }
}
