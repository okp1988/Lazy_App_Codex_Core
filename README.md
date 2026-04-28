Important: You just need adb for this. (You can use scrcpy to mirror screen to your  PC)

Note: Use adb to control your phone from PC without installing any app on your phone. (No root needed)

1) Download adb and put at C:\adb
2) Use USB Debugging / Wireless Debugging to connect
	
	i) Wireless debugging : Important Command

		a) adb connect <IP_ADDRESS>:PORT (Connect device)

		b) adb pairs <IP_ADDRESS>:PORT (Pair device)

		c) adb devices (Check connected device list)

		d) adb disconnect <IP_ADDRESS>:PORT (Remove certain device)

		e) adb start-server (Start adb server)

		f) adb kill-server (Kill adb server)
