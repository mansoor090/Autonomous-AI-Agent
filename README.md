# 🚀 Unity Autonomous Player Navigation with Reinforcement Learning






## 🎯 Project Overview

This project demonstrates an Autonomous Player capable of navigating a dynamic environment within Unity, leveraging advanced Reinforcement Learning (RL) techniques. Using Python, Gymnasium, and Unity's C# scripting, the agent learns to efficiently move toward randomized targets while avoiding obstacles.

## 🌟 Features

- **Custom Gym Environment:** Built using Gymnasium and integrated seamlessly with Unity.

- **PPO Algorithm:** Efficient learning via Stable-Baselines3's Proximal Policy Optimization.

- **RPC Communication:** Robust JSON-RPC integration between Unity (C#) and Python scripts.

- **Dynamic Navigation:** Autonomous behavior in response to randomized goals and obstacle placement.

## 🛠️ Technologies Used

- Unity Engine (C# scripting)

- Python with Gymnasium

- Stable-Baselines3 (PPO algorithm)

- peaceful_pie (Unity-Python RPC communication), Thanks to Hugo RL

## 🚧 Setup & Installation

### **Clone this repository:**

`git clone <repo-link>`

### **Install Python dependencies:**

`pip install gymnasium stable-baselines3 peaceful_pie`

Open Unity project, import necessary scripts, and ensure the correct RPC ports are configured.

### **🕹️ Running the Project**

#### Training the Agent

`python train_rl.py --port 9000`

#### Testing the Agent

`python test_rl.py --model PPO_Test_Model_1 --episodes 50`

### 📺 Demo

// Sooon Picture will be here. 
### 📈 Results

The trained agent successfully navigates dynamically generated environments, effectively demonstrating RL's capability to adapt and improve navigation strategies autonomously.

### 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the issues page or open a pull request.

### 📜 License
None - Free to Use & Fork

Made with ❤️ by Mansoor