import chatIcon from "../styles/chaticon.png";

export default function AppLogo({ size = 73, showText = true }) {
  return (
    <div style={{ display: "flex", alignItems: "left", gap: 10, marginLeft:-20 }}>
      <img
        src={chatIcon}
        alt="KittyCatQuant"
        style={{ width: size, height: size, objectFit: "contain" }}
      />
      {showText && (
        <div
          style={{
            fontFamily: "Inter, sans-serif",
            fontWeight: 400,
            fontSize: 24,
            letterSpacing: "-1.5px", 
            marginLeft:-15 ,
            alignContent:"center",
            color: "rgba(231,238,252,0.95)",
          }}
        >
          KittyCatQuant
        </div>
      )}
    </div>
  );
}