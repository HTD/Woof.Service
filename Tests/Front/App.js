class App {

    async start() {
        let response = await fetch("http://localhost:12345/test/Hello", {
            method: "get",
            mode: "cors",
            credentials: "include"
        });
        let responseData = await response.json();
        console.dir(responseData);
    }

}