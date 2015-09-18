using System;
using System.IO;
using System.Collections.Generic;

namespace SharpSsh.jsch
{
    class UserAuthPublicKey : UserAuth
    {
        internal UserInfo m_userinfo;

        internal UserAuthPublicKey(UserInfo userinfo)
        {
            m_userinfo = userinfo;
        }

        public override bool start(Session session)
        {
            List<Identity> identities = session.m_jsch.Identities;

            Packet packet = session.m_packet;
            Buffer buf = session.m_buf;

            string passphrase = null;
            string username = session.m_username;

            byte[] _username = null;
            try { _username = Util.getBytesUTF8(username); }
            catch
            {
                _username = Util.getBytes(username);
            }

            for (int i = 0; i < identities.Count; i++)
            {
                Identity identity = (Identity)(identities[i]);
                byte[] pubkeyblob = identity.PublicKeyBlob;

                if (pubkeyblob != null)
                {
                    // send
                    // byte      SSH_MSG_USERAUTH_REQUEST(50)
                    // string    user name
                    // string    service name ("ssh-connection")
                    // string    "publickey"
                    // boolen    FALSE
                    // string    plaintext password (ISO-10646 UTF-8)
                    packet.reset();
                    buf.putByte((byte)Session.SSH_MSG_USERAUTH_REQUEST);
                    buf.putString(_username);
                    buf.putString(Util.getBytes("ssh-connection"));
                    buf.putString(Util.getBytes("publickey"));
                    buf.putByte((byte)0);
                    buf.putString(Util.getBytes(identity.AlgName));
                    buf.putString(pubkeyblob);
                    session.write(packet);

                loop1:
                    while (true)
                    {
                        // receive
                        // byte      SSH_MSG_USERAUTH_PK_OK(52)
                        // string    service name
                        buf = session.read(buf);
                        if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_PK_OK)
                            break;
                        else if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_FAILURE)
                            break;
                        else if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_BANNER)
                        {
                            buf.getInt(); buf.getByte(); buf.getByte();
                            byte[] _message = buf.getString();
                            byte[] lang = buf.getString();
                            string message = null;
                            try
                            {
                                message = Util.getStringUTF8(_message);
                            }
                            catch
                            {
                                message = Util.getString(_message);
                            }
                            if (m_userinfo != null)
                                m_userinfo.showMessage(message);
                            goto loop1;
                        }
                        else
                            break;
                    }
                    if (buf.m_buffer[5] != Session.SSH_MSG_USERAUTH_PK_OK)
                        continue;
                }

                int count = 5;
                while (true)
                {
                    if ((identity.isEncrypted && passphrase == null))
                    {
                        if (m_userinfo == null)
                            throw new JSchException("USERAUTH fail");
                        if (identity.isEncrypted && !m_userinfo.promptPassphrase("Passphrase for " + identity.Name))
                            throw new JSchAuthCancelException("publickey");

                        passphrase = m_userinfo.Passphrase;
                    }

                    if (!identity.isEncrypted || passphrase != null)
                    {
                        if (identity.setPassphrase(passphrase))
                            break;
                    }
                    passphrase = null;
                    count--;
                    if (count == 0)
                        break;
                }

                if (identity.isEncrypted)
                    continue;
                if (pubkeyblob == null)
                    pubkeyblob = identity.PublicKeyBlob;
                if (pubkeyblob == null)
                    continue;

                // send
                // byte      SSH_MSG_USERAUTH_REQUEST(50)
                // string    user name
                // string    service name ("ssh-connection")
                // string    "publickey"
                // boolen    TRUE
                // string    plaintext password (ISO-10646 UTF-8)
                packet.reset();
                buf.putByte((byte)Session.SSH_MSG_USERAUTH_REQUEST);
                buf.putString(_username);
                buf.putString(Util.getBytes("ssh-connection"));
                buf.putString(Util.getBytes("publickey"));
                buf.putByte((byte)1);
                buf.putString(Util.getBytes(identity.AlgName));
                buf.putString(pubkeyblob);

                byte[] sid = session.getSessionId();
                uint sidlen = (uint)sid.Length;
                byte[] tmp = new byte[4 + sidlen + buf.m_index - 5];
                tmp[0] = (byte)(sidlen >> 24);
                tmp[1] = (byte)(sidlen >> 16);
                tmp[2] = (byte)(sidlen >> 8);
                tmp[3] = (byte)(sidlen);
                Array.Copy(sid, 0, tmp, 4, sidlen);
                Array.Copy(buf.m_buffer, 5, tmp, 4 + sidlen, buf.m_index - 5);

                byte[] signature = identity.getSignature(session, tmp);
                if (signature == null)  // for example, too long key length.
                    break;

                buf.putString(signature);
                session.write(packet);

            loop2:
                while (true)
                {
                    // receive
                    // byte      SSH_MSG_USERAUTH_SUCCESS(52)
                    // string    service name
                    buf = session.read(buf);
                    if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_SUCCESS)
                        return true;
                    else if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_BANNER)
                    {
                        buf.getInt(); buf.getByte(); buf.getByte();
                        byte[] _message = buf.getString();
                        byte[] lang = buf.getString();
                        string message = null;
                        try { message = Util.getStringUTF8(_message); }
                        catch
                        {
                            message = Util.getString(_message);
                        }
                        if (m_userinfo != null)
                            m_userinfo.showMessage(message);
                        goto loop2;
                    }
                    else if (buf.m_buffer[5] == Session.SSH_MSG_USERAUTH_FAILURE)
                    {
                        buf.getInt(); buf.getByte(); buf.getByte();
                        byte[] foo = buf.getString();
                        int partial_success = buf.getByte();
                        if (partial_success != 0)
                            throw new JSchPartialAuthException(Util.getString(foo));
                        break;
                    }
                    break;
                }
            }
            return false;
        }
    }
}
