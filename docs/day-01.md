# First walkthrough: from a request to a balance

1. Run the app, sign in as Alice and open the new-request form.
2. Trace `Login` in `AccountController`: verify the password, create identity claims, sign the cookie.
3. Read the role attributes in `LeaveController`. Explain why hiding a button alone cannot protect a manager action.
4. Follow `Create` into `LeaveBook.Submit`. Which rules run before a request is stored?
5. Submit a two-weekday request. Explain why available days drop from 20 to 18 while it is pending.
6. Sign in as Manager and approve it. Explain why available stays 18 and approved becomes 2.
7. Create another request and reject it. Check that only its reserved days return.
8. Restart the app. Explain why requests disappear and what SQL must change.

## Your first change

Add a clear employee-facing explanation beside the available balance. Run the existing checks and commit the change yourself:

```sh
git clone https://github.com/ViktorAtanasov08/leave-manager.git
cd leave-manager
git switch -c learn/balance-explanation
# edit, run and check the application
git add Views/Leave/Index.cshtml
git commit -m "Explain how pending leave affects the available balance"
git push -u origin learn/balance-explanation
```

Open a pull request on GitHub and explain your change in your own words. The scaffold was AI-assisted; being able to explain and change it is the learning goal.
