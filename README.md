![GitHub release (latest by date)](https://img.shields.io/github/v/release/Mysttic/AIDemon2)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/Mysttic/AIDemon2?include_prereleases)

# AIDemon2 - your personal AI assistant

![AIDemon2](https://github.com/user-attachments/assets/adf0c421-980d-42c7-a3db-ba903d47b441)

**This application allows to interact with various available AI models and helps write and execute scripts on your computer.**

## About this project

With this app you will be able to quickly communicate with the AI ​​model to prepare a script to be executed on your device.

Thanks to predefined communication instructions and language preferences, you will receive a ready-to-run script that does not require special formatting, copying, pasting, or creating a new file for it. 

Just specify in the message what functionality you expect. The selected AI model will prepare this script in the language you provided and send it back to you in response.

## Prerequisites
The application uses AI models provided by the **io.net** platform. 
To run the application, you must create an account there and obtain an API key that will be used in the application to communicate with selected AI models.
![image](https://github.com/user-attachments/assets/019d2c6b-b8ce-4192-8a19-f819489e36c6)
https://ai.io.net/ai/models

## Supported programming languages
| Language | Supported | Example |
|:--------:|:-------:|:-------:|
| <img src="https://github.com/user-attachments/assets/8000a7f8-5880-4fab-be01-6fa41e32bbe9" height="48" alt="python"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/ad3fe48d-7e75-48fd-8f48-265c7db3c463" height="48" alt="powershell"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/04aae282-f452-4840-a0b8-c4510b2a4146" height="48" alt="batch"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/1b23d960-4a3a-4e94-a9cc-2831cf91b102" height="48" alt="nodejs"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/40f642d6-c132-4e78-8925-be68cc8074ae" height="48" alt="bash"> | <img src="https://github.com/user-attachments/assets/6330a48d-70a1-4738-8e3c-affb63e92c0a" height="24" alt="inprogress"> |  |
| <img src="https://github.com/user-attachments/assets/b8a9b6cc-06e2-4751-9d41-ae6e6086769f" height="48" alt="zsh"> | <img src="https://github.com/user-attachments/assets/6330a48d-70a1-4738-8e3c-affb63e92c0a" height="24" alt="inprogress"> |  |
| <img src="https://github.com/user-attachments/assets/9e12b9b2-80dd-4c40-afe6-3ef09b38092e" height="48" alt="perl"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/7cbb44f4-36ad-4c57-9948-6ee0e65a39a7" height="48" alt="ruby"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/70dd0bf6-4d99-4a05-8a19-aeef84c5f568" height="48" alt="php"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/5199af82-a7f1-4867-a731-304960b013fc" height="48" alt="groovy"> | <img src="https://github.com/user-attachments/assets/6330a48d-70a1-4738-8e3c-affb63e92c0a" height="24" alt="inprogress"> |  |
| <img src="https://github.com/user-attachments/assets/8faf05cb-9e13-46ab-b148-14be937d9d4e" height="48" alt="lua"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |
| <img src="https://github.com/user-attachments/assets/5c3fa94a-60b9-46c6-9a10-9498e98c2811" height="48" alt="go"> | <img src="https://github.com/user-attachments/assets/7cf707a8-2987-43df-bb68-038e53b9c8e6" height="24" alt="approved"> |  |

## Application UI
![image](https://github.com/user-attachments/assets/5fab1ef0-5b8a-4657-8118-cac22bd817ae)
The application interface consists of the following components:
- The chat window displays user messages and responses from the AI ​​model. 

- Field for entering a message
- Button for sending a message

- Expandable panel on the left side consisting of the following elements:
  - Button for collapsing/expanding the panel
  - Button for opening the settings window
  - Button for exporting all messages
  - Button for clearing history deleting all messages
  - List of saved messages

- Expandable panel on the right side consisting of the following elements:
  - Button for collapsing/expanding the panel
  - Save button adding a given message to the list of saved messages
  - Run button which runs the code of a given message, works only for messages received from AI
  - Resend button allowing for re-sending a message, works only for user messages
  - Button for exporting a message to a script file, works only for messages received from AI and creates a script with an extension appropriate for the given language
  - Button for deleting a message from the saved list, does not completely delete the message
  - Button for clearing the message editing window
  - Field containing the content of the message in which it can be edited
  - Console output field displaying what the console returns when executing the sent script

## Configuration
![image](https://github.com/user-attachments/assets/e1ee3862-3ab2-42a0-b7a0-cdde14dfd650)
Once you receive the key from the **io.net** platform, in the application settings you must paste the API key required for communication with this platform.

If you want the communication with the AI ​​model to proceed on the basis that in the received response you will receive a ready-to-execute script, you must define the content of the instruction that will be sent to the model before sending the actual message from the user. In the Instruction Prompt field, you can freely define the content at your own discretion, an example of the instruction content:

```
Your task is to write scripts in the given scripting language, the purpose of which is to perform operations in my Windows system. Your answers should contain only ready code that can be pasted into the script and run. You are not to provide any confirmations, explanations or anything other than the code you are to write. You can include additional information in comments in the script. In each script, add a short comment at the beginning describing the script.
```

The platform provides many different AI models that you can communicate with, in the AIModel field you have this list available to choose from. If you wants to change the model, you can freely change it here.

If you want the AI ​​to generate scripts, you must specify in which language it should write it. In the Programming Language field, there is a selection list available, you must select the language before starting communication.

## How to use

![image](https://github.com/user-attachments/assets/76a8c988-dcbf-459a-a574-a52c60e66167)
Once everything is set up, you can start communicating. Just write a message and click 'send'. Depending on the model, you might wait some time for a response. After receiving it, it will be added to the list in the chat window. In the window with the response, you can see the information about which model sent us this response, it will be the same model that you selected in the settings.

![image](https://github.com/user-attachments/assets/aee88303-6368-42d9-ae23-97b3b0aa9273)
After receiving the response, you can open the message by double-clicking on it, which will cause the message editing window to slide out on the right.

Here you can trigger the script to run, making any necessary corrections to the code. Run the script by clicking on the run icon, which causes the code to be transferred from the edit field to the script file that is run. The result of operation will be visible in the console window after it has finished.

## Disclaimer
The authors of the solution are not responsible for the quality and content generated by the AI models, and do not take responsibility for the effects of invoking scripts generated by it.

The solution and its authors are in no way affiliated with owners of **io.net** platform.

For more information on regulations, please see the licensing arrangements.
