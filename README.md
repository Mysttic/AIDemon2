![GitHub release (latest by date)](https://img.shields.io/github/v/release/Mysttic/AIDemon2)
![GitHub release (latest by date)](https://img.shields.io/github/v/release/Mysttic/AIDemon2?include_prereleases)

# AIDemon2 - your personal AI assistant

![AIDemon2](https://github.com/user-attachments/assets/adf0c421-980d-42c7-a3db-ba903d47b441)

**The application will allow you to interact with the various AI models available and help you write and execute scripts on your computer.**

## About this project

Using this application, you will be able to quickly communicate with an AI model to prepare a script for execution on your device.

Thanks to predefined communication instructions and language preferences, you will receive a ready-to-run script that does not require special formatting, copying, pasting, or creating a new file for it. 

Your task will be to specify in the message what functionality you expect so that, upon receiving the response, you can quickly execute it and obtain the result.

## Prerequisites
The application uses AI models provided by the io.net platform. 
To run the application, you must create an account there and obtain an API key that will be used in the application to communicate with selected AI models.
![image](https://github.com/user-attachments/assets/019d2c6b-b8ce-4192-8a19-f819489e36c6)
https://ai.io.net/ai/models

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
Once we receive the key from the **io.net** platform, in the application settings we paste the API key required for communication with this platform.

If we want the communication with the AI ​​model to proceed on the basis that in the received response we are to receive a ready-to-execute script, we must define the content of the instruction that will be sent to the model before sending the actual message from the user. In the Instruction Prompt field, you can freely define the content at your own discretion, an example of the instruction content:

```
Your task is to write scripts in the given scripting language, the purpose of which is to perform operations in my Windows system. Your answers should contain only ready code that can be pasted into the script and run. You are not to provide any confirmations, explanations or anything other than the code you are to write. You can include additional information in comments in the script. In each script, add a short comment at the beginning describing the script.
```

The platform provides many different AI models that we can communicate with, in the AIModel field we have this list available to choose from. If the user wants to change the model, they can freely change it here.

If we want the AI ​​to generate scripts for us, we must specify in which language it should write them. In the Programming Language field, there is a selection list available, we must select the language before starting communication.


