# Release process

1. Build the plugin.
2. If there is no release branch yet for the current version:  
   `nbgv prepare-release`
3. Switch to the release branch:  
   `git switch release/v1.2`
4. Push the release branch
5. Create a tag and push it afterwards:
    - `nbgv tag`
    - `git push origin v1.2`
6. Create a release in GitHub from the tag and attach the file `PropertyServer.dll`.
7. Push the main branch.
