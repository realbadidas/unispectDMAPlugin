using System;
using Unispect;
using vmmsharp;

namespace MyMemoryNameSpace
{
    [UnispectPlugin]
    public sealed class DMAMemoryPlugin : MemoryProxy
    {
        Vmm vmm;
        uint pid;
        bool connected = false;
        public DMAMemoryPlugin()
        {
            if (connected)
                return;
            Log.Add("DMA Plugin starting...");
            if (!connectToDMA())
                throw new Exception("VMM Init Failed");
            
        }

        private bool connectToDMA()
        {
            Log.Add("Connecting to device...");

            try
            {
                vmm = new Vmm("-norefresh", "-v", "-device", "FPGA");
                Log.Add("Connected to device!");
                connected = true;
            }
            catch (Exception ex)
            {
                Log.Add("Failed to init");
                connected = false;
            }
            return connected;
        }

        public override ModuleProxy GetModule(string moduleName)
        {
            Log.Add("[DMA] Getting module..");
            // This method is only used to get the base address and size in memory of 'moduleName' (mono-2.0-bdwgc.dll by default)
            // The inspector target is obtained by using that module.


            //ulong baseAddress = vmm.ProcessGetModuleBase(pid, moduleName);
            Vmm.MAP_MODULEENTRY module = vmm.Map_GetModuleFromName(pid, moduleName);

            Log.Add("[DMA] Module: Search: " + moduleName + "" + " | Found: " + module.wszText + " | BaseAddr: " + module.vaBase + " | Size: " + module.cbImageSize);
            //if(module.vaBase != 0)
                return new ModuleProxy(moduleName, module.vaBase, (int)module.cbImageSize);

            // If the module is not found then return null so we can attempt to locate alternatives
            //return null;
        }

        public override bool AttachToProcess(string handle)
        {
            Log.Add("[DMA] Attaching to process");
            // Attach to the process so that the two Read functions are able to interface with the process.
            // The argument: handle (string) will be the text from Unispect's "Process Handle" text box.
            return vmm.PidGetFromName(handle, out pid);
        }

        public override byte[] Read(ulong address, int length)
        {
            // This handles reading bytes into a byte array.
            byte[] output;
            try
            {
                //Log.Add("READING " + address + " WITH SIZE " + length);
                output = vmm.MemRead(pid, address, (uint)length);

                //Fix if memread does not read properly
                if (output.Length != length)
                {
                    byte[] fix = new byte[length];
                    for(int i = 0; i < output.Length; i++)
                    {
                        fix[i] = output[i];
                    }
                    output = fix;
                }
            } catch(Exception ex)
            {
                Log.Error("[DMA] Error whilst reading memory!");
                throw new Exception(ex.Message, ex);
            }
            return output;
        }

        public override void Dispose()
        {
            Log.Add("[DMA] Dispose");
            connected = false;
            vmm.Close();
            vmm.Dispose();
            // Cleanup. Close native handles, free unmanaged memory, or anything else the garbage collector won't see.
        }
    }

}