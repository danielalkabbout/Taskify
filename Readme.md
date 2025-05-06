how to setup the project:
1. Clone the repository from:https://AdvancedProgrammingCsh@dev.azure.com/AdvancedProgrammingCsh/Advanced%20Programming/_git/Advanced%20Programming
or you can open the downloaded zip file and extract it then do the following steps
1. open the project in visual studio
2. open the terminal in visual studio
3. run the following command to install the required packages
dotnet build
4.if you have mamp run the project directly, if you are using xampp comment the line in the appsettings.json file and uncomment the commented part
5. Run the project

 To test the apis you can use swagger or postman
you can already test the api by using these json below 

POST /api/auth/register  
{
  "firstName": "admin",
  "lastName": "admin",
  "email": "admin@admin.com",
  "password": "Admin123!",
  "role": "Admin"
},
{
  "firstName": "daniel",
  "lastName": "kabbout",
  "email": "danielk@manager.com",
  "password": "Manager123!",
  "role": "Manager"
},
{
  "firstName": "sevak",
  "lastName": "Mazmanian",
  "email": "sevakm@user.com",
  "password": "User123!",
  "role": "Employee"
}




POST /api/auth/login 
{
  "email": "admin@admin.com",
  "password": "Admin123!",
}
A jwt token will appear, it should be added in the authorize form so it can be authorized and to use the apis

Categories
POST /api/categories 
{
  "name": "Dev"
} 
Notification

POST /api/notifications 
{
  "userId": "92081872-b50b-4460-a725-639c0996024d",
  "message": "hello"
}
POST /api/notifications/{id}/mark-as-read
Enter the Id of the notification and it will be marked as read


Task Management
POST /api/tasks  

{
  "title": "Implement user authentication",
  "description": "set up JWT authentication for the API endpoints",
  "dueDate": "2025-06-06T21:22:49.723Z",
  "status": "Pending",
  "priority": "High",
  "categoryId": 1,
  "reminderDate": "2025-05-08T21:22:49.723Z",
  "recurrence": "Weekly",
  "assignedUserIds": [
    "92081872-b50b-4460-a725-639c0996024d"
  ]
}

Task Comment
POST /api/tasks/{taskId}/comments 
enter the task id and then add a message
{
  "text": "need to be dine as soon as possible"
}
